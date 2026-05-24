using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Controller de procedimientos, etapas y artefactos esperados.
///
/// Endpoints:
///   GET  /api/procedimientos                          → todos los procedimientos
///   GET  /api/procedimientos/{id}/etapas              → etapas de un procedimiento
///   GET  /api/procedimientos/etapas/{id}/artefactos   → artefactos de una etapa
///   POST /api/procedimientos/artefactos               → crear artefacto (Admin)
///   PUT  /api/procedimientos/artefactos/{id}          → modificar artefacto (Admin)
/// </summary>
[ApiController]
[Route("api/procedimientos")]
[Authorize]
public class ProcedimientoController : ControllerBase
{
    private readonly ProcedimientoService _service;
    private readonly ILogger<ProcedimientoController> _logger;

    public ProcedimientoController(ProcedimientoService service, ILogger<ProcedimientoController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ── PROCEDIMIENTOS ────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve todos los procedimientos.
    /// Cualquier usuario autenticado puede verlos.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos()
    {
        var procedimientos = await _service.ObtenerTodosAsync();
        return Ok(procedimientos);
    }

    /// <summary>
    /// Devuelve un procedimiento especifico por ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var procedimiento = await _service.ObtenerPorIdAsync(id);

        if (procedimiento == null)
            return NotFound(new { mensaje = $"Procedimiento con ID {id} no encontrado" });

        return Ok(procedimiento);
    }

    // ── ETAPAS ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve las etapas de un procedimiento con sus artefactos.
    /// </summary>
    [HttpGet("{id}/etapas")]
    public async Task<IActionResult> ObtenerEtapas(int id)
    {
        var etapas = await _service.ObtenerEtapasAsync(id);
        return Ok(etapas);
    }

    // ── ARTEFACTOS ESPERADOS ──────────────────────────────────────────────────

    /// <summary>
    /// Devuelve los artefactos esperados de una etapa.
    /// </summary>
    [HttpGet("etapas/{etapaId}/artefactos")]
    public async Task<IActionResult> ObtenerArtefactos(int etapaId)
    {
        var artefactos = await _service.ObtenerArtefactosAsync(etapaId);
        return Ok(artefactos);
    }

    /// <summary>
    /// Crea un artefacto esperado nuevo.
    /// Solo el Administrador puede crear artefactos.
    /// </summary>
    [HttpPost("artefactos")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> CrearArtefacto([FromBody] CrearArtefactoRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var artefacto = await _service.CrearArtefactoAsync(request);
        return CreatedAtAction(nameof(ObtenerArtefactos),
            new { etapaId = artefacto.EtapaId }, artefacto);
    }

    /// <summary>
    /// Modifica un artefacto esperado existente.
    /// Solo el Administrador puede modificar artefactos.
    /// </summary>
    [HttpPut("artefactos/{id}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ModificarArtefacto(int id, [FromBody] ModificarArtefactoRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var artefacto = await _service.ModificarArtefactoAsync(id, request);

        if (artefacto == null)
            return NotFound(new { mensaje = $"Artefacto con ID {id} no encontrado" });

        return Ok(artefacto);
    }
}