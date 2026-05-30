using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de informes usando Entity Framework + MySQL.
/// </summary>
public class InformeRepository : IInformeRepository
{
    private readonly ISOAuditAgentDbContext _db;
    private readonly ILogger<InformeRepository> _logger;

    public InformeRepository(ISOAuditAgentDbContext db, ILogger<InformeRepository> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Informe>> ObtenerTodosAsync()
    {
        try
        {
            return await _db.Informes
                //.Where(i => i.Activo)
                .OrderByDescending(i => i.FechaGeneracion)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los informes");
            return [];
        }
    }

    public async Task<IReadOnlyList<Informe>> ObtenerPorAuditoriaAsync(int auditoriaId)
    {
        try
        {
            return await _db.Informes
                .Where(i => i.AuditoriaId == auditoriaId)
                .OrderByDescending(i => i.FechaGeneracion)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener informes de auditoria: {Id}", auditoriaId);
            return [];
        }
    }

    public async Task<Informe?> ObtenerPorIdAsync(int id)
    {
        try
        {
            return await _db.Informes
                .FirstOrDefaultAsync(i => i.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener informe: {Id}", id);
            return null;
        }
    }

    public async Task<Informe> CrearAsync(Informe informe)
    {
        _db.Informes.Add(informe);
        await _db.SaveChangesAsync();
        return informe;
    }

    public async Task<Informe> ActualizarAsync(Informe informe)
    {
        _db.Informes.Update(informe);
        await _db.SaveChangesAsync();
        return informe;
    }
}