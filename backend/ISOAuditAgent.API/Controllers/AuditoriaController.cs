using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Controller de gestion de auditorias.
///
/// Endpoints:
///   GET  /api/auditorias                    → todas las auditorias (Admin) 
///   GET  /api/auditorias/proyecto/{id}      → auditorias de un proyecto
///   GET  /api/auditorias/{id}               → una auditoria especifica
///   POST /api/auditorias                    → crear auditoria
///   PUT  /api/auditorias/{id}/estado        → actualizar estado (Admin)
/// </summary>
[ApiController]
[Route("api/auditorias")]
[Authorize] // Todos los endpoints requieren estar autenticado
public class AuditoriaController : ControllerBase
{
    private readonly AuditoriaService _auditoriaService;
    private readonly ILogger<AuditoriaController> _logger;

    public AuditoriaController(AuditoriaService auditoriaService, ILogger<AuditoriaController> logger)
    {
        _auditoriaService = auditoriaService;
        _logger           = logger;
    }

    // ── CONSULTAS ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve todas las auditorias.
    /// Solo el Administrador puede ver todas.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ObtenerTodas()
    {
        var auditorias = await _auditoriaService.ObtenerTodasAsync();
        return Ok(auditorias);
    }

    /// <summary>
    /// Devuelve las auditorias de un proyecto especifico.
    /// </summary>
    [HttpGet("proyecto/{proyectoId}")]
    public async Task<IActionResult> ObtenerPorProyecto(int proyectoId)
    {
        var auditorias = await _auditoriaService.ObtenerPorProyectoAsync(proyectoId);
        return Ok(auditorias);
    }

    /// <summary>
    /// Devuelve una auditoria especifica por ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var auditoria = await _auditoriaService.ObtenerPorIdAsync(id);

        if (auditoria == null)
            return NotFound(new { mensaje = $"Auditoria con ID {id} no encontrada" });

        return Ok(auditoria);
    }

    // ── CREAR Y EJECUTAR ──────────────────────────────────────────────────────

    /// <summary>
    /// Crea una auditoria nueva y la inicia.
    /// Cualquier usuario autenticado puede crear una auditoria.
    /// El ID del auditor se toma del token JWT.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearAuditoriaRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Obtenemos el ID del usuario desde el token JWT
        var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (idClaim == null || !int.TryParse(idClaim, out var usuarioId))
            return Unauthorized();

        try
        {
            var auditoria = await _auditoriaService.CrearAsync(request, usuarioId);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = auditoria.Id }, auditoria);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza el estado de una auditoria.
    /// Lo llama el orquestador cuando el workflow de agentes termina.
    /// Solo el Administrador puede actualizar estados.
    /// </summary>
    [HttpPut("{id}/estado")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] string estado)
    {
        // Validar que el estado sea valido
        if (estado != EstadosAuditoria.Completada && estado != EstadosAuditoria.Fallida)
            return BadRequest(new { mensaje = $"Estado invalido: {estado}. Debe ser 'completada' o 'fallida'" });

        var auditoria = await _auditoriaService.ActualizarEstadoAsync(id, estado);

        if (auditoria == null)
            return NotFound(new { mensaje = $"Auditoria con ID {id} no encontrada" });

        return Ok(auditoria);
    }
}