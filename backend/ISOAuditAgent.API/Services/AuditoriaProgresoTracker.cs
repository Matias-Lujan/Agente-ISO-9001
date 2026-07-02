// ============================================================================
//  AuditoriaProgresoTracker — Escritura del progreso del workflow
// ----------------------------------------------------------------------------
//  Cada nodo LLM del orquestador llama a este servicio al empezar y al
//  terminar. Las filas alimentan GET /api/auditorias/{id}/progreso, que el
//  frontend polea para mostrar el avance.
//
//  POR QUÉ NO USA EL DbContext SCOPED DEL RUNNER:
//  El AuditoriaRunner crea un IServiceScope por auditoría. Si el tracker
//  reutilizara ese DbContext:
//    1. Los SaveChangesAsync intermedios del tracker tocarían el mismo
//       changetracker que la transacción de persistencia del resultado, lo
//       que invita a inconsistencias y warnings sobre "operación pendiente".
//    2. El nodo final (ConsolidadorResultado) y el AuditoriaPersistenceService
//       abren una transacción explícita sobre ese DbContext. Si el tracker
//       intenta escribir mientras esa transacción está abierta, mezcla
//       lecturas/escrituras de progreso con la transacción del resultado.
//
//  Para evitar todo eso, este servicio es Singleton, recibe IServiceScopeFactory
//  por constructor, y cada operación de escritura abre su propio scope
//  → su propio DbContext → su propio commit. Totalmente desacoplado de la
//  transacción del runner.
//
//  CONCURRENCIA:
//  El worker procesa una auditoría por vez (AuditoriaWorkerService). Pero
//  ComplianceValidation y ConsistencyVerification corren en paralelo dentro
//  de una misma auditoría (fan-out en el workflow). Por eso cada operación
//  usa un DbContext fresco — DbContext no es thread-safe.
// ============================================================================

using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Services;

public interface IAuditoriaProgresoTracker
{
    /// <summary>
    /// Inserta una fila Pendiente por cada nodo del workflow para esta
    /// auditoría. La llama el AuditoriaRunner antes de arrancar el workflow.
    /// Idempotente: si ya existen filas para esa auditoría, no las duplica.
    /// </summary>
    Task InicializarPendientesAsync(int auditoriaId, CancellationToken ct);

    Task MarcarEnCursoAsync(int auditoriaId, NodoWorkflow nodo, CancellationToken ct);
    Task MarcarCompletadoAsync(int auditoriaId, NodoWorkflow nodo, CancellationToken ct);
    Task MarcarFallidoAsync(int auditoriaId, NodoWorkflow nodo, CancellationToken ct);

    /// <summary>
    /// Marca como Fallido cualquier nodo que quedó en estado EnCurso para esta
    /// auditoría. La llama el AuditoriaRunner en el camino de error, porque
    /// el catch del runner no sabe qué nodo específico falló.
    /// </summary>
    Task MarcarTodosEnCursoComoFallidosAsync(int auditoriaId, CancellationToken ct);
}

public sealed class AuditoriaProgresoTracker : IAuditoriaProgresoTracker
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditoriaProgresoTracker> _logger;

    public AuditoriaProgresoTracker(
        IServiceScopeFactory scopeFactory,
        ILogger<AuditoriaProgresoTracker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InicializarPendientesAsync(int auditoriaId, CancellationToken ct)
    {
        await EjecutarSeguroAsync(auditoriaId, "InicializarPendientes", async db =>
        {
            var existentes = await db.AuditoriaProgresos
                .Where(p => p.AuditoriaId == auditoriaId)
                .Select(p => p.Nodo)
                .ToListAsync(ct);

            var faltantes = Enum.GetValues<NodoWorkflow>()
                .Except(existentes)
                .Select(nodo => new AuditoriaProgreso
                {
                    AuditoriaId = auditoriaId,
                    Nodo = nodo,
                    Estado = EstadoProgreso.Pendiente
                })
                .ToList();

            if (faltantes.Count == 0) return;

            db.AuditoriaProgresos.AddRange(faltantes);
            await db.SaveChangesAsync(ct);
        });
    }

    public Task MarcarEnCursoAsync(int auditoriaId, NodoWorkflow nodo, CancellationToken ct) =>
        ActualizarEstadoAsync(auditoriaId, nodo, EstadoProgreso.EnCurso, ct);

    public Task MarcarCompletadoAsync(int auditoriaId, NodoWorkflow nodo, CancellationToken ct) =>
        ActualizarEstadoAsync(auditoriaId, nodo, EstadoProgreso.Completado, ct);

    public Task MarcarFallidoAsync(int auditoriaId, NodoWorkflow nodo, CancellationToken ct) =>
        ActualizarEstadoAsync(auditoriaId, nodo, EstadoProgreso.Fallido, ct);

    public async Task MarcarTodosEnCursoComoFallidosAsync(
        int auditoriaId, CancellationToken ct)
    {
        await EjecutarSeguroAsync(auditoriaId, "MarcarTodosEnCursoComoFallidos", async db =>
        {
            var enCurso = await db.AuditoriaProgresos
                .Where(p => p.AuditoriaId == auditoriaId && p.Estado == EstadoProgreso.EnCurso)
                .ToListAsync(ct);

            if (enCurso.Count == 0) return;

            var ahora = DateTime.UtcNow;
            foreach (var fila in enCurso)
            {
                fila.Estado = EstadoProgreso.Fallido;
                fila.FechaFinUtc = ahora;
            }

            await db.SaveChangesAsync(ct);
        });
    }

    private async Task ActualizarEstadoAsync(
        int auditoriaId, NodoWorkflow nodo, EstadoProgreso nuevoEstado, CancellationToken ct)
    {
        await EjecutarSeguroAsync(auditoriaId, $"Actualizar {nodo}={nuevoEstado}", async db =>
        {
            var fila = await db.AuditoriaProgresos
                .FirstOrDefaultAsync(p => p.AuditoriaId == auditoriaId && p.Nodo == nodo, ct);

            if (fila is null)
            {
                // No debería pasar: el runner inicializa todas las filas antes
                // de arrancar el workflow. Si pasa, la insertamos para no perder
                // la señal y logueamos warning.
                fila = new AuditoriaProgreso
                {
                    AuditoriaId = auditoriaId,
                    Nodo = nodo,
                    Estado = nuevoEstado
                };
                db.AuditoriaProgresos.Add(fila);

                _logger.LogWarning(
                    "Tracker: no se encontró fila Pendiente para auditoría {AuditoriaId} " +
                    "nodo {Nodo}. Se inserta directamente como {Estado}.",
                    auditoriaId, nodo, nuevoEstado);
            }
            else
            {
                fila.Estado = nuevoEstado;
            }

            var ahora = DateTime.UtcNow;
            if (nuevoEstado == EstadoProgreso.EnCurso)
            {
                fila.FechaInicioUtc = ahora;
                fila.FechaFinUtc = null;
            }
            else if (nuevoEstado is EstadoProgreso.Completado or EstadoProgreso.Fallido)
            {
                fila.FechaFinUtc = ahora;
            }

            await db.SaveChangesAsync(ct);
        });
    }

    // Cada operación corre en su propio scope/DbContext. Si la escritura del
    // progreso falla, logueamos pero NO relanzamos: el progreso es accesorio
    // y no debe tumbar la auditoría.
    private async Task EjecutarSeguroAsync(
        int auditoriaId, string operacion, Func<ISOAuditAgentDbContext, Task> trabajo)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISOAuditAgentDbContext>();
            await trabajo(db);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Tracker falló en operación '{Operacion}' para auditoría {AuditoriaId}. " +
                "Se ignora — el progreso es accesorio, no debe afectar la auditoría.",
                operacion, auditoriaId);
        }
    }
}
