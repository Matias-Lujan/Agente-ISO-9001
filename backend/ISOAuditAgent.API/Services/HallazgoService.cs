using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Servicio de hallazgos.
/// Solo lectura — los hallazgos los insertan los agentes.
/// </summary>
public class HallazgoService
{
    private readonly IHallazgoRepository _repo;
    private readonly ILogger<HallazgoService> _logger;

    public HallazgoService(IHallazgoRepository repo, ILogger<HallazgoService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<HallazgoResponse>> ObtenerPorAuditoriaAsync(int auditoriaId)
    {
        var hallazgos = await _repo.ObtenerPorAuditoriaAsync(auditoriaId);
        return hallazgos.Select(MapearHallazgo).ToList();
    }

    public async Task<HallazgoResponse?> ObtenerPorIdAsync(int id)
    {
        var hallazgo = await _repo.ObtenerPorIdAsync(id);
        return hallazgo == null ? null : MapearHallazgo(hallazgo);
    }

    private static HallazgoResponse MapearHallazgo(Hallazgo h) => new(
        h.Id,
        h.ArtefactoEvaluado.AuditoriaId,
        h.ArtefactoEvaluadoId,
        h.ArtefactoEvaluado.ArtefactoEsperado.Nombre,
        h.Tipo.ToString(),
        h.Descripcion,
        h.Justificacion,
        h.AgenteOrigen.ToString());
}