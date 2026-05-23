// ============================================================================
//  AuditoriaRepository — Operaciones sobre la entidad Auditoria
// ----------------------------------------------------------------------------
//  Implementa IAuditoriaRepository. Consumidores:
//   - API REST (D6): CrearEnCursoAsync, ObtenerPorIdOrNullAsync.
//   - AuditoriaRunner: ObtenerPorIdOrNullAsync, MarcarFallidaAsync.
//   - AuditoriaPersistenceService: MarcarCompletadaAsync (dentro de la
//     transacción del UnitOfWork).
//
//  Notas:
//   - MarcarCompletada y MarcarFallida llaman SaveChangesAsync por sí mismos.
//     Cuando se ejecutan dentro de EjecutarEnTransaccionAsync (caso de
//     MarcarCompletada), el UoW agrupa esos SaveChanges en el commit único.
//   - MarcarFallida se llama FUERA de transacción (camino de error del
//     worker). SaveChangesAsync directo está bien — no hay rollback que
//     coordinar.
//   - Si la auditoría no existe al marcarla, lanzamos InvalidOperationException:
//     no debería ocurrir nunca (la API la creó antes de encolar); si pasa,
//     es un bug y queremos enterarnos ruidosamente.
// ============================================================================

using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

public sealed class AuditoriaRepository : IAuditoriaRepository
{
    private readonly ISOAuditAgentDbContext _context;

    public AuditoriaRepository(ISOAuditAgentDbContext context)
    {
        _context = context;
    }

    public async Task<int> CrearEnCursoAsync(
        int proyectoId, int usuarioId, int etapaId, CancellationToken ct)
    {
        var auditoria = new Auditoria
        {
            ProyectoId = proyectoId,
            UsuarioId = usuarioId,
            EtapaId = etapaId,
            FechaInicioUtc = DateTime.UtcNow,
            FechaFinalizacionUtc = null,
            Estado = EstadoAuditoria.EnCurso
        };

        _context.Auditorias.Add(auditoria);
        await _context.SaveChangesAsync(ct);

        // EF Core puebla auditoria.Id tras el SaveChanges (auto-increment).
        return auditoria.Id;
    }

    public Task<Auditoria?> ObtenerPorIdOrNullAsync(int auditoriaId, CancellationToken ct)
    {
        // AsNoTracking: los consumidores actuales no modifican la entidad
        // por esta vía. Cuando hace falta modificarla (Marcar*), se busca
        // de nuevo con tracking.
        return _context.Auditorias
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == auditoriaId, ct);
    }

    public async Task MarcarCompletadaAsync(int auditoriaId, CancellationToken ct)
    {
        var auditoria = await _context.Auditorias
            .FirstOrDefaultAsync(a => a.Id == auditoriaId, ct)
            ?? throw new InvalidOperationException(
                $"No se puede marcar como Completada la auditoría {auditoriaId}: " +
                $"no existe en la BD.");

        auditoria.Estado = EstadoAuditoria.Completada;
        auditoria.FechaFinalizacionUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }

    public async Task MarcarFallidaAsync(int auditoriaId, CancellationToken ct)
    {
        var auditoria = await _context.Auditorias
            .FirstOrDefaultAsync(a => a.Id == auditoriaId, ct)
            ?? throw new InvalidOperationException(
                $"No se puede marcar como Fallida la auditoría {auditoriaId}: " +
                $"no existe en la BD.");

        auditoria.Estado = EstadoAuditoria.Fallida;
        auditoria.FechaFinalizacionUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }
}
