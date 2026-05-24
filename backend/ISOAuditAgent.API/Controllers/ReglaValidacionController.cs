using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Controller de reglas de validacion.
///
/// Endpoints de consulta — los usa el orquestador:
///   GET /api/reglas/procedimiento/{id}  → reglas de un procedimiento
///   GET /api/reglas/etapa/{id}          → reglas de una etapa especifica
///
/// Endpoints de gestion — solo Administrador:
///   POST /api/reglas                    → crear regla nueva
///   PUT  /api/reglas/{id}               → modificar regla existente
/// </summary>
[ApiController]
[Route("api/reglas")]
[Authorize]
public class ReglaValidacionController : ControllerBase
{
    private readonly ReglaValidacionService _service;
    private readonly ILogger<ReglaValidacionController> _logger;

    public ReglaValidacionController(
        ReglaValidacionService service,
        ILogger<ReglaValidacionController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ── CONSULTAS — cualquier usuario autenticado ─────────────────────────────

    /// <summary>
    /// Devuelve todas las reglas activas de un procedimiento.
    /// El orquestador llama a esto para saber que validar.
    /// </summary>
    [HttpGet("procedimiento/{procedimientoId}")]
    public async Task<IActionResult> ObtenerPorProcedimiento(int procedimientoId)
    {
        var reglas = await _service.ObtenerPorProcedimientoAsync(procedimientoId);
        return Ok(reglas);
    }

    /// <summary>
    /// Devuelve las reglas activas de una etapa especifica.
    /// El orquestador llama a esto al auditar una etapa particular.
    /// </summary>
    [HttpGet("etapa/{etapaId}")]
    public async Task<IActionResult> ObtenerPorEtapa(int etapaId)
    {
        var reglas = await _service.ObtenerPorEtapaAsync(etapaId);
        return Ok(reglas);
    }

    // ── GESTION — solo Administrador ─────────────────────────────────────────

    /// <summary>
    /// Crea una regla nueva.
    /// Solo el Administrador puede crear reglas.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Crear([FromBody] CrearReglaRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var regla = await _service.CrearAsync(request);
            return CreatedAtAction(
                nameof(ObtenerPorProcedimiento),
                new { procedimientoId = regla.ProcedimientoId },
                regla);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Modifica una regla existente.
    /// Solo el Administrador puede modificar reglas.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Modificar(int id, [FromBody] ModificarReglaRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var regla = await _service.ModificarAsync(id, request);

        if (regla == null)
            return NotFound(new { mensaje = $"Regla con ID {id} no encontrada" });

        return Ok(regla);
    }
}