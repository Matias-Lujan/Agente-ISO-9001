using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Controller de gestion de informes de auditoria.
///
/// Endpoints:
///   GET  /api/informes                          → todos los informes (Admin)
///   GET  /api/informes/auditoria/{auditoriaId}  → informes de una auditoria
///   GET  /api/informes/{id}                     → un informe especifico
///   POST /api/informes/manual                   → generar informe manual
///   POST /api/informes/automatico/{auditoriaId} → generar informe automatico (Admin)
/// </summary>
[ApiController]
[Route("api/informes")]
[Authorize]
public class InformeController : ControllerBase
{
    private readonly InformeService _informeService;
    private readonly ILogger<InformeController> _logger;

    public InformeController(InformeService informeService, ILogger<InformeController> logger)
    {
        _informeService = informeService;
        _logger         = logger;
    }

    // ── CONSULTAS ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve todos los informes.
    /// Administrador y Auditor pueden ver todos.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{Roles.Administrador},{Roles.Auditor}")]
    public async Task<IActionResult> ObtenerTodos()
    {
        var informes = await _informeService.ObtenerTodosAsync();
        return Ok(informes);
    }

    /// <summary>
    /// Devuelve los informes de una auditoria especifica.
    /// Cualquier usuario autenticado puede verlos.
    /// </summary>
    [HttpGet("auditoria/{auditoriaId}")]
    public async Task<IActionResult> ObtenerPorAuditoria(int auditoriaId)
    {
        var informes = await _informeService.ObtenerPorAuditoriaAsync(auditoriaId);
        return Ok(informes);
    }

    /// <summary>
    /// Devuelve un informe especifico por ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var informe = await _informeService.ObtenerPorIdAsync(id);

        if (informe == null)
            return NotFound(new { mensaje = $"Informe con ID {id} no encontrado" });

        return Ok(informe);
    }

    // ── GENERAR INFORMES ──────────────────────────────────────────────────────

    /// <summary>
    /// Genera un informe manual bajo demanda.
    /// Cualquier usuario autenticado puede generarlo.
    /// Solo funciona para auditorias completadas.
    /// </summary>
    [HttpPost("manual")]
    public async Task<IActionResult> GenerarManual([FromBody] GenerarInformeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var informe = await _informeService.GenerarManualAsync(request);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = informe.Id }, informe);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Genera un informe automatico cuando el workflow de agentes termina.
    /// Solo el Administrador puede llamar a este endpoint.
    /// En el sistema completo lo llama el orquestador, no el usuario.
    /// </summary>
    [HttpPost("automatico/{auditoriaId}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> GenerarAutomatico(int auditoriaId)
    {
        try
        {
            var informe = await _informeService.GenerarAutomaticoAsync(auditoriaId);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = informe.Id }, informe);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }
}