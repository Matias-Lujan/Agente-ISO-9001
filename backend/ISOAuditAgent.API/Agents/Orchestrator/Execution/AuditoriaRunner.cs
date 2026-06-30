
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Repositories;
using ISOAuditAgent.API.Services;
using Microsoft.Agents.AI.Workflows;

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

        // Inicializar las filas de progreso ANTES de arrancar el workflow para
        // que el endpoint GET /api/auditorias/{id}/progreso ya tenga algo que
        // devolver desde el primer polling del frontend (todos los nodos en
        // estado Pendiente). El tracker es defensivo: si falla no tumba la
        // auditoría.
        var tracker = sp.GetRequiredService<IAuditoriaProgresoTracker>();
        await tracker.InicializarPendientesAsync(auditoriaId, ct);

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

            // El nodo que estaba EnCurso al momento de la excepción debería
            // haberse marcado a sí mismo como Fallido (en su catch). Esta es
            // red de seguridad por si el catch del nodo no llegó a correr
            // (ej. excepción fuera del try interno o cancelación abrupta).
            await tracker.MarcarTodosEnCursoComoFallidosAsync(auditoriaId, CancellationToken.None);

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
        // API MAF validada — ejecuta el workflow en proceso y consume el stream.
        var run = await InProcessExecution.RunStreamingAsync(workflow, input);

        AuditoriaResultado? resultado = null;

        await foreach (var evento in run.WatchStreamAsync().WithCancellation(ct))
        {
            switch (evento)
            {
                case WorkflowOutputEvent salida:
                    // AuditoriaResultado. En la API MAF usada, el dato llega por evento.Data.
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
    /// En la API MAF usada, el resultado del nodo de salida llega en evento.Data.
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
    /// En la API MAF usada, el error del workflow expone evento.Exception.
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
