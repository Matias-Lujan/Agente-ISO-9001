using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de procedimientos usando Entity Framework + MySQL.
/// </summary>
public class ProcedimientoRepository : IProcedimientoRepository
{
    private readonly ISOAuditAgentDbContext _db;
    private readonly ILogger<ProcedimientoRepository> _logger;

    public ProcedimientoRepository(ISOAuditAgentDbContext db, ILogger<ProcedimientoRepository> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Procedimiento>> ObtenerTodosAsync()
    {
        try
        {
            return await _db.Procedimientos
                .OrderBy(p => p.Codigo)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener procedimientos");
            return [];
        }
    }

    public async Task<Procedimiento?> ObtenerPorIdAsync(int id)
    {
        try
        {
            return await _db.Procedimientos
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener procedimiento: {Id}", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<Etapa>> ObtenerEtapasPorProcedimientoAsync(int procedimientoId)
    {
        try
        {
            return await _db.Etapas
                .Where(e => e.ProcedimientoId == procedimientoId)
                .OrderBy(e => e.Orden)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener etapas del procedimiento: {Id}", procedimientoId);
            return [];
        }
    }

    public async Task<Etapa?> ObtenerEtapaPorIdAsync(int id)
    {
        try
        {
            return await _db.Etapas
                .FirstOrDefaultAsync(e => e.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener etapa: {Id}", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<ArtefactoEsperado>> ObtenerArtefactosPorEtapaAsync(int etapaId)
    {
        try
        {
            return await _db.ArtefactosEsperados
                .Where(a => a.EtapaId == etapaId)
                .OrderBy(a => a.Nombre)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener artefactos de la etapa: {Id}", etapaId);
            return [];
        }
    }

    public async Task<ArtefactoEsperado?> ObtenerArtefactoPorIdAsync(int id)
    {
        try
        {
            return await _db.ArtefactosEsperados
                .FirstOrDefaultAsync(a => a.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener artefacto: {Id}", id);
            return null;
        }
    }

    public async Task<ArtefactoEsperado> CrearArtefactoAsync(ArtefactoEsperado artefacto)
    {
        _db.ArtefactosEsperados.Add(artefacto);
        await _db.SaveChangesAsync();
        return artefacto;
    }

    public async Task<ArtefactoEsperado> ActualizarArtefactoAsync(ArtefactoEsperado artefacto)
    {
        _db.ArtefactosEsperados.Update(artefacto);
        await _db.SaveChangesAsync();
        return artefacto;
    }

    // ── Usados por el workflow del orchestrator (portado de dev) ──────────────

    public async Task<IReadOnlyList<Etapa>> ObtenerEtapasAsync(int procedimientoId, CancellationToken ct)
    {
        // Include obligatorio: ResolutorContextoService usa etapaAuditada.Procedimiento.Codigo/Nombre.
        return await _db.Etapas
            .AsNoTracking()
            .Include(e => e.Procedimiento)
            .Where(e => e.ProcedimientoId == procedimientoId)
            .OrderBy(e => e.Orden)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ArtefactoEsperado>> ObtenerArtefactosEsperadosAsync(
        int procedimientoId, CancellationToken ct)
    {
        // Include obligatorio: ResolutorContextoService usa ae.Etapa.Orden.
        return await _db.ArtefactosEsperados
            .AsNoTracking()
            .Include(ae => ae.Etapa)
            .Where(ae => ae.Etapa.ProcedimientoId == procedimientoId)
            .ToListAsync(ct);
    }
}