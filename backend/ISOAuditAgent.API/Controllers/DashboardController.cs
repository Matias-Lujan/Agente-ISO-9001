using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Dashboard del auditor: una sola llamada agregada, calculada en el backend por
/// AnalyticsService (misma regla que /api/hallazgos/revision → no pueden discrepar).
/// Solo Administrador / Auditor, igual que la pantalla.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.Auditor}")]
public class DashboardController : ControllerBase
{
    private readonly AnalyticsService _analytics;

    public DashboardController(AnalyticsService analytics) => _analytics = analytics;

    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken ct)
    {
        // Admin / Auditor ven todos los proyectos → sin filtro por ids.
        var dashboard = await _analytics.ObtenerDashboardAsync(null, ct);
        return Ok(dashboard);
    }
}
