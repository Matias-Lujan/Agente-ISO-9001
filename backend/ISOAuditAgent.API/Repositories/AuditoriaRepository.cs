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
}