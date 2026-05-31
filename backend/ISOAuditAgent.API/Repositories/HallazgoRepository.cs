using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de hallazgos usando Entity Framework.
/// Solo lectura — los hallazgos los insertan los agentes.
/// </summary>
public class HallazgoRepository : IHallazgoRepository
{
    private readonly ISOAuditAgentDbContext _db;
    private readonly ILogger<HallazgoRepository> _logger;

    public HallazgoRepository(ISOAuditAgentDbContext db, ILogger<HallazgoRepository> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Hallazgo>> ObtenerPorAuditoriaAsync(int auditoriaId)
    {
        try
        {
            return await _db.Hallazgos
                .Where(h => h.ArtefactoEvaluado.AuditoriaId == auditoriaId)
                .Include(h => h.ArtefactoEvaluado)
                    .ThenInclude(ae => ae.ArtefactoEsperado)
                .OrderBy(h => h.Tipo)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener hallazgos de auditoria: {Id}", auditoriaId);
            return [];
        }
    }

    public async Task<Hallazgo?> ObtenerPorIdAsync(int id)
    {
        try
        {
            return await _db.Hallazgos
                .Include(h => h.ArtefactoEvaluado)
                    .ThenInclude(ae => ae.ArtefactoEsperado)
                .FirstOrDefaultAsync(h => h.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener hallazgo: {Id}", id);
            return null;
        }
    }
}