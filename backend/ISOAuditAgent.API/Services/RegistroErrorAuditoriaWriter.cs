// ============================================================================
//  RegistroErrorAuditoriaWriter — Escritura del log durable de errores
// ----------------------------------------------------------------------------
//  Persiste una fila en registros_error_auditoria por cada fallo. A diferencia
//  de los logs de consola, esto sobrevive reinicios y es consultable.
//
//  Sigue el mismo patrón que AuditoriaProgresoTracker (ver ese archivo para el
//  detalle): Singleton, recibe IServiceScopeFactory, y cada escritura abre su
//  propio scope → su propio DbContext → su propio commit, desacoplado de la
//  transacción de persistencia del runner. Es defensivo: si la escritura del
//  error falla, se loguea pero NUNCA se relanza — registrar el error no debe,
//  a su vez, tumbar el camino de manejo de error.
// ============================================================================

using ISOAuditAgent.API.Agents.Orchestrator;
using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Services;

public interface IRegistroErrorAuditoriaWriter
{
    /// <summary>
    /// Clasifica la excepción y persiste una fila de error para el nodo indicado.
    /// La llaman los nodos del workflow en su catch. Defensivo: no lanza.
    /// </summary>
    Task RegistrarAsync(int auditoriaId, NodoWorkflow? nodo, Exception ex, CancellationToken ct);

    /// <summary>
    /// Registra un error "de red de seguridad" a nivel auditoría (Nodo = null),
    /// pero SOLO si todavía no hay ninguna fila de error para esa auditoría. La
    /// llama el runner: si un nodo ya registró el fallo puntual, no duplicamos;
    /// si el fallo ocurrió fuera de un nodo (resolución de contexto, persistencia),
    /// esta es la única que lo captura. Defensivo: no lanza.
    /// </summary>
    Task RegistrarFallbackAsync(int auditoriaId, Exception ex, CancellationToken ct);
}

public sealed class RegistroErrorAuditoriaWriter : IRegistroErrorAuditoriaWriter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegistroErrorAuditoriaWriter> _logger;

    public RegistroErrorAuditoriaWriter(
        IServiceScopeFactory scopeFactory,
        ILogger<RegistroErrorAuditoriaWriter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task RegistrarAsync(int auditoriaId, NodoWorkflow? nodo, Exception ex, CancellationToken ct) =>
        EscribirSeguroAsync(auditoriaId, async db =>
        {
            db.RegistrosErrorAuditoria.Add(Construir(auditoriaId, nodo, ex));
            await db.SaveChangesAsync(ct);
        });

    public Task RegistrarFallbackAsync(int auditoriaId, Exception ex, CancellationToken ct) =>
        EscribirSeguroAsync(auditoriaId, async db =>
        {
            var yaHay = await db.RegistrosErrorAuditoria
                .AnyAsync(r => r.AuditoriaId == auditoriaId, ct);
            if (yaHay) return;

            db.RegistrosErrorAuditoria.Add(Construir(auditoriaId, nodo: null, ex));
            await db.SaveChangesAsync(ct);
        });

    private static RegistroErrorAuditoria Construir(int auditoriaId, NodoWorkflow? nodo, Exception ex)
    {
        var (categoria, mensaje) = ClasificadorErrorAuditoria.Clasificar(ex);
        return new RegistroErrorAuditoria
        {
            AuditoriaId = auditoriaId,
            Nodo = nodo,
            Categoria = categoria,
            Mensaje = mensaje,
            DetalleTecnico = ex.ToString(),
            FechaUtc = DateTime.UtcNow
        };
    }

    private async Task EscribirSeguroAsync(
        int auditoriaId, Func<ISOAuditAgentDbContext, Task> trabajo)
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
                "No se pudo persistir el registro de error de la auditoría {AuditoriaId}. " +
                "El error original igual quedó en los logs.", auditoriaId);
        }
    }
}
