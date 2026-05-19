// ============================================================================
//  AuditoriaRunner — Ejecución de una auditoría de punta a punta
// ----------------------------------------------------------------------------
//  Recibe un AuditoriaId (de una auditoría ya creada en estado EnCurso por la
//  API) y la ejecuta completa:
//
//    1. Crea un IServiceScope propio de esta auditoría.
//    2. Resuelve los 6 nodos DENTRO del scope (instancias frescas).
//    3. Consulta la Auditoria para armar el IniciarAuditoriaWorkflowInput.
//    4. Arma el workflow con AuditoriaWorkflowFactory.
//    5. Lo corre y consume el stream de eventos.
//    6. Éxito  -> AuditoriaPersistenceService persiste y marca Completada.
//       Error  -> marca Fallida.
//
//  Por qué un scope nuevo por auditoría:
//   - FindingsClassificationNode cachea estado: necesita instancia fresca.
//   - El DbContext de EF Core y los repositorios son Scoped.
//   El worker corre fuera de un request HTTP, así que no hay un scope
//   ambiente: hay que crearlo explícitamente con IServiceScopeFactory.
//
//  INVARIANTE DURO: ninguna auditoría queda colgada en EnCurso. Pase lo que
//  pase —excepción en un nodo, WorkflowErrorEvent, fallo de persistencia— la
//  auditoría termina en Completada o Fallida.
//
//  API MAF: verificar — InProcessExecution.RunStreamingAsync, WatchStreamAsync
//  y los tipos de evento (WorkflowOutputEvent, WorkflowErrorEvent) contra el
//  paquete 1.3.0. Coinciden con el spike y la doc revisada; el detalle fino
//  (cómo se extrae el dato de WorkflowOutputEvent) se confirma al compilar.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Repositories;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ISOAuditAgent.API.Agents.Orchestrator;

/// <summary>
/// Ejecuta una auditoría. Lo invoca el AuditoriaWorkerService por cada
/// AuditoriaId que toma de la cola. Se registra como Singleton: no tiene
/// estado propio; el estado por-auditoría vive en el scope que crea.
/// </summary>
public sealed class AuditoriaRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditoriaRunner> _logger;

    public AuditoriaRunner(
        IServiceScopeFactory scopeFactory,
        ILogger<AuditoriaRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta la auditoría indicada. No lanza: cualquier error se traduce a
    /// estado Fallida. El worker puede llamar a esto sin envolverlo en try.
    /// </summary>
    public async Task EjecutarAsync(int auditoriaId, CancellationToken ct)
    {
        // Scope propio de esta auditoría. Al salir del using se liberan el
        // DbContext, los repositorios y los 6 nodos.
        using IServiceScope scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        try
        {
            await EjecutarEnScopeAsync(auditoriaId, sp, ct);

            _logger.LogInformation(
                "Auditoría {AuditoriaId} completada con éxito.", auditoriaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Auditoría {AuditoriaId} falló. Se marca como Fallida.",
                auditoriaId);

            await MarcarFallidaSeguroAsync(auditoriaId, sp, ct);
        }
    }

    // ------------------------------------------------------------------------
    //  Camino de ejecución (puede lanzar; el catch de arriba lo traduce)
    // ------------------------------------------------------------------------

    private async Task EjecutarEnScopeAsync(
        int auditoriaId, IServiceProvider sp, CancellationToken ct)
    {
        // --- 1. Armar el input del workflow desde la Auditoria --------------
        var auditoriaRepo = sp.GetRequiredService<IAuditoriaRepository>();

        var auditoria = await auditoriaRepo.ObtenerPorIdOrNullAsync(auditoriaId, ct)
            ?? throw new InvalidOperationException(
                $"La auditoría {auditoriaId} no existe en BD. No se puede " +
                $"ejecutar el workflow.");

        var input = new IniciarAuditoriaWorkflowInput(
            AuditoriaId: auditoria.Id,
            ProyectoId: auditoria.ProyectoId,
            EtapaId: auditoria.EtapaId);

        // --- 2. Resolver los 6 nodos dentro del scope -----------------------
        var nodos = new NodosAuditoria(
            Resolutor: sp.GetRequiredService<ResolutorContextoNode>(),
            DocumentAnalysis: sp.GetRequiredService<DocumentAnalysisNode>(),
            ComplianceValidation: sp.GetRequiredService<ComplianceValidationNode>(),
            ConsistencyVerification: sp.GetRequiredService<ConsistencyVerificationNode>(),
            FindingsClassification: sp.GetRequiredService<FindingsClassificationNode>(),
            Consolidador: sp.GetRequiredService<ConsolidadorResultadoNode>());

        // --- 3. Armar el workflow -------------------------------------------
        Workflow workflow = AuditoriaWorkflowFactory.Crear(nodos);

        // --- 4. Correr y consumir el stream de eventos ----------------------
        AuditoriaResultado resultado = await CorrerWorkflowAsync(workflow, input, ct);

        // --- 5. Persistir (marca Completada dentro de la transacción) -------
        var persistencia = sp.GetRequiredService<IAuditoriaPersistenceService>();
        await persistencia.PersistirResultadoAsync(resultado, ct);
    }

    /// <summary>
    /// Corre el workflow y consume el stream hasta obtener el AuditoriaResultado.
    /// Si el workflow emite un WorkflowErrorEvent, lanza para que el camino de
    /// error lo capture.
    /// </summary>
    private async Task<AuditoriaResultado> CorrerWorkflowAsync(
        Workflow workflow,
        IniciarAuditoriaWorkflowInput input,
        CancellationToken ct)
    {
        // API MAF: verificar — firma de RunStreamingAsync / WatchStreamAsync
        // contra el paquete 1.3.0.
        var run = await InProcessExecution.RunStreamingAsync(workflow, input);

        AuditoriaResultado? resultado = null;

        await foreach (var evento in run.WatchStreamAsync().WithCancellation(ct))
        {
            switch (evento)
            {
                case WorkflowOutputEvent salida:
                    // El nodo de salida (ConsolidadorResultado) emitió el
                    // AuditoriaResultado. API MAF: verificar cómo se extrae el
                    // dato del evento (.Data, .Output, cast directo...).
                    resultado = ExtraerResultado(salida);
                    break;

                case WorkflowErrorEvent error:
                    // El workflow falló internamente (excepción en un nodo:
                    // ContextoAuditoriaException, EnsambleAuditoriaException,
                    // o cualquier otra). Se relanza para el camino de error.
                    throw new InvalidOperationException(
                        $"El workflow de la auditoría {input.AuditoriaId} " +
                        $"emitió un WorkflowErrorEvent.",
                        ExtraerExcepcion(error));
            }
        }

        // Si el stream terminó sin WorkflowOutputEvent, el workflow no produjo
        // resultado. Es una condición anómala.
        return resultado
            ?? throw new InvalidOperationException(
                $"El workflow de la auditoría {input.AuditoriaId} terminó sin " +
                $"emitir un AuditoriaResultado.");
    }

    // ------------------------------------------------------------------------
    //  Extracción de datos de los eventos (dependiente de la API de MAF)
    // ------------------------------------------------------------------------

    /// <summary>
    /// Extrae el AuditoriaResultado de un WorkflowOutputEvent.
    /// API MAF: verificar — la propiedad exacta que lleva el dato del evento.
    /// </summary>
    private static AuditoriaResultado ExtraerResultado(WorkflowOutputEvent evento)
    {
        // Se asume que el evento expone el dato tipado. Si la propiedad tiene
        // otro nombre o requiere cast, ajustar acá.
        if (evento.Data is AuditoriaResultado resultado)
            return resultado;

        throw new InvalidOperationException(
            $"WorkflowOutputEvent no contenía un AuditoriaResultado. " +
            $"Contenía: {evento.Data?.GetType().Name ?? "null"}.");
    }

    /// <summary>
    /// Extrae la excepción de un WorkflowErrorEvent, si la expone.
    /// API MAF: verificar — la propiedad exacta. Puede devolver null.
    /// </summary>
    private static Exception? ExtraerExcepcion(WorkflowErrorEvent evento)
    {
        // Se asume que el evento expone la excepción. Si no, devolver null:
        // el camino de error igual marca Fallida, solo pierde el detalle.
        return evento.Exception;
    }

    // ------------------------------------------------------------------------
    //  Marcado de Fallida — defensivo
    // ------------------------------------------------------------------------

    /// <summary>
    /// Marca la auditoría como Fallida. Envuelto en su propio try/catch: si
    /// hasta esto falla (BD caída, etc.), se loguea pero no se relanza —
    /// relanzar acá no aportaría nada y taparía el error original.
    /// </summary>
    private async Task MarcarFallidaSeguroAsync(
        int auditoriaId, IServiceProvider sp, CancellationToken ct)
    {
        try
        {
            var auditoriaRepo = sp.GetRequiredService<IAuditoriaRepository>();
            await auditoriaRepo.MarcarFallidaAsync(auditoriaId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "No se pudo marcar la auditoría {AuditoriaId} como Fallida. " +
                "Queda en EnCurso en la BD y requiere intervención manual.",
                auditoriaId);
        }
    }
}
