using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de auditorias usando Entity Framework + MySQL.
/// </summary>
public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly ISOAuditAgentDbContext _db;
    private readonly ILogger<AuditoriaRepository> _logger;

    public AuditoriaRepository(ISOAuditAgentDbContext db, ILogger<AuditoriaRepository> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Auditoria>> ObtenerTodasAsync()
    {
        try
        {
            return await _db.Auditorias
                //.Where(a => a.Activo)
                .OrderByDescending(a => a.FechaInicioUtc)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las auditorias");
            return [];
        }
    }

    public async Task<IReadOnlyList<Auditoria>> ObtenerPorProyectoAsync(int proyectoId)
    {
        try
        {
            return await _db.Auditorias
                .Where(a => a.ProyectoId == proyectoId)
                .OrderByDescending(a => a.FechaInicioUtc)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener auditorias del proyecto: {Id}", proyectoId);
            return [];
        }
    }

    public async Task<Auditoria?> ObtenerPorIdAsync(int id)
    {
        try
        {
            return await _db.Auditorias
                .FirstOrDefaultAsync(a => a.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener auditoria por ID: {Id}", id);
            return null;
        }
    }

    public async Task<Auditoria> CrearAsync(Auditoria auditoria)
    {
        _db.Auditorias.Add(auditoria);
        await _db.SaveChangesAsync();
        return auditoria;
    }

    public async Task<Auditoria> ActualizarAsync(Auditoria auditoria)
    {
        _db.Auditorias.Update(auditoria);
        await _db.SaveChangesAsync();
        return auditoria;
    }

    // ── Usados por el workflow del orchestrator (portado de dev) ──────────────

    public async Task<int> CrearEnCursoAsync(
        int proyectoId, int usuarioId, int etapaId, CancellationToken ct)
    {
        var auditoria = new Auditoria
        {
            ProyectoId           = proyectoId,
            UsuarioId            = usuarioId,
            EtapaId              = etapaId,
            FechaInicioUtc       = DateTime.UtcNow,
            FechaFinalizacionUtc = null,
            Estado               = EstadoAuditoria.EnCurso,
            Activo               = true,
        };

        _db.Auditorias.Add(auditoria);
        await _db.SaveChangesAsync(ct);
        return auditoria.Id;
    }

    public Task<Auditoria?> ObtenerPorIdOrNullAsync(int auditoriaId, CancellationToken ct)
    {
        return _db.Auditorias
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == auditoriaId, ct);
    }

    public async Task MarcarCompletadaAsync(int auditoriaId, CancellationToken ct)
    {
        var auditoria = await _db.Auditorias
            .FirstOrDefaultAsync(a => a.Id == auditoriaId, ct)
            ?? throw new InvalidOperationException(
                $"No se puede marcar como Completada la auditoría {auditoriaId}: no existe.");

        auditoria.Estado               = EstadoAuditoria.Completada;
        auditoria.FechaFinalizacionUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarcarFallidaAsync(
        int auditoriaId, CategoriaErrorAuditoria categoria, string mensaje, CancellationToken ct)
    {
        var auditoria = await _db.Auditorias
            .FirstOrDefaultAsync(a => a.Id == auditoriaId, ct)
            ?? throw new InvalidOperationException(
                $"No se puede marcar como Fallida la auditoría {auditoriaId}: no existe.");

        auditoria.Estado               = EstadoAuditoria.Fallida;
        auditoria.FechaFinalizacionUtc = DateTime.UtcNow;
        auditoria.CategoriaError       = categoria;
        auditoria.MensajeError         = mensaje;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RegistroErrorAuditoria>> ObtenerErroresAsync(
        int auditoriaId, CancellationToken ct)
    {
        return await _db.RegistrosErrorAuditoria
            .AsNoTracking()
            .Where(r => r.AuditoriaId == auditoriaId)
            .OrderByDescending(r => r.FechaUtc)
            .ThenByDescending(r => r.Id)
            .ToListAsync(ct);
    }

    public Task<Auditoria?> ObtenerConResultadoOrNullAsync(int id, CancellationToken ct)
    {
        return _db.Auditorias
            .AsNoTracking()
            .Include(a => a.ArtefactosEvaluados)
                .ThenInclude(ae => ae.ArtefactoEsperado)
            .Include(a => a.ArtefactosEvaluados)
                .ThenInclude(ae => ae.Hallazgos)
            .Include(a => a.ArtefactosEvaluados)
                .ThenInclude(ae => ae.DocumentosAnalizados)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }
}