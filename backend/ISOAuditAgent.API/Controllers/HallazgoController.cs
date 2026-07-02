using System.Security.Claims;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

[ApiController]
[Route("api/hallazgos")]
[Authorize]
public class HallazgoController : ControllerBase
{
    private readonly HallazgoService _service;
    private readonly AnalyticsService _analytics;
    private readonly IProyectoRepository _proyectoRepo;

    public HallazgoController(
        HallazgoService service,
        AnalyticsService analytics,
        IProyectoRepository proyectoRepo)
    {
        _service      = service;
        _analytics    = analytics;
        _proyectoRepo = proyectoRepo;
    }

    /// <summary>
    /// Revisión consolidada de hallazgos (pantalla Hallazgos). Una sola llamada,
    /// scopeada por rol (el Operador ve solo sus proyectos asignados).
    ///   scope=actual     (default) → última auditoría Completada de cada proyecto
    ///   scope=historico            → todas las auditorías Completada
    /// </summary>
    [HttpGet("revision")]
    public async Task<IActionResult> ObtenerRevision([FromQuery] string? scope, CancellationToken ct)
    {
        var alcance = string.Equals(scope, "historico", StringComparison.OrdinalIgnoreCase)
            ? AlcanceAnalitica.Historico
            : AlcanceAnalitica.Actual;

        var idClaim  = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var rolClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (idClaim is null || !int.TryParse(idClaim, out var usuarioId))
            return Unauthorized();

        // Admin / Auditor ven todo; el Operador solo sus proyectos asignados.
        var puedeVerTodos = rolClaim == Roles.Administrador || rolClaim == Roles.Auditor;
        IReadOnlyList<int>? ids = null;
        if (!puedeVerTodos)
        {
            var proys = await _proyectoRepo.ObtenerPorUsuarioAsync(usuarioId);
            ids = proys.Select(p => p.Id).ToList();
        }

        var revision = await _analytics.ObtenerRevisionAsync(alcance, ids, ct);
        return Ok(revision);
    }

    [HttpGet("auditoria/{auditoriaId:int}")]
    public async Task<IActionResult> ObtenerPorAuditoria(int auditoriaId)
    {
        var hallazgos = await _service.ObtenerPorAuditoriaAsync(auditoriaId);
        return Ok(hallazgos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var hallazgo = await _service.ObtenerPorIdAsync(id);
        return hallazgo is null ? NotFound() : Ok(hallazgo);
    }
}
