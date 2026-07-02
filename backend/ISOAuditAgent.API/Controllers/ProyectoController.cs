using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Controller de gestion de proyectos.
///
/// Endpoints:
///   GET    /api/proyectos              → todos los proyectos (Admin) o los del usuario
///   GET    /api/proyectos/{id}         → un proyecto especifico
///   POST   /api/proyectos              → crear proyecto (Admin)
///   PUT    /api/proyectos/{id}         → modificar proyecto (Admin)
///   POST   /api/proyectos/{id}/responsables          → asignar responsable (Admin)
///   DELETE /api/proyectos/{id}/responsables/{userId} → quitar responsable (Admin)
/// </summary>
[ApiController]
[Route("api/proyectos")]
[Authorize] // Todos los endpoints requieren estar autenticado
public class ProyectoController : ControllerBase
{
    private readonly ProyectoService _proyectoService;
    private readonly ILogger<ProyectoController> _logger;

    public ProyectoController(ProyectoService proyectoService, ILogger<ProyectoController> logger)
    {
        _proyectoService = proyectoService;
        _logger          = logger;
    }

    // ── CONSULTAS ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve proyectos segun el rol del usuario:
    /// - Administrador / Auditor → todos los proyectos
    /// - Operador → solo sus proyectos asignados
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObtenerProyectos()
    {
        // Obtenemos el ID y rol del usuario desde el token JWT
        var idClaim  = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var rolClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (idClaim == null || !int.TryParse(idClaim, out var usuarioId))
            return Unauthorized();

        // El Administrador y el Auditor ven todos los proyectos.
        // El Operador ve solo los que tiene asignados.
        var puedeVerTodos = rolClaim == Roles.Administrador || rolClaim == Roles.Auditor;
        var proyectos = puedeVerTodos
            ? await _proyectoService.ObtenerTodosAsync()
            : await _proyectoService.ObtenerPorUsuarioAsync(usuarioId);

        return Ok(proyectos);
    }

    /// <summary>
    /// Devuelve un proyecto especifico por ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var proyecto = await _proyectoService.ObtenerPorIdAsync(id);

        if (proyecto == null)
            return NotFound(new { mensaje = $"Proyecto con ID {id} no encontrado" });

        return Ok(proyecto);
    }

    // ── CREAR Y MODIFICAR ─────────────────────────────────────────────────────

    /// <summary>
    /// Crea un proyecto nuevo.
    /// Solo el Administrador puede crear proyectos.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Crear([FromBody] CrearProyectoRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var proyecto = await _proyectoService.CrearAsync(request);
            return CreatedAtAction(nameof(ObtenerPorId), new { id = proyecto.Id }, proyecto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Modifica un proyecto existente.
    /// Solo el Administrador puede modificar proyectos.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> Modificar(int id, [FromBody] ModificarProyectoRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var proyecto = await _proyectoService.ModificarAsync(id, request);

        if (proyecto == null)
            return NotFound(new { mensaje = $"Proyecto con ID {id} no encontrado" });

        return Ok(proyecto);
    }

    // ── RESPONSABLES ──────────────────────────────────────────────────────────

    /// <summary>
    /// Asigna un usuario como responsable de un proyecto.
    /// Solo el Administrador puede asignar responsables.
    /// </summary>
    [HttpPost("{id}/responsables")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> AsignarResponsable(int id, [FromBody] AsignarResponsableRequest request)
    {
        try
        {
            await _proyectoService.AsignarResponsableAsync(id, request.UsuarioId);
            return Ok(new { mensaje = "Responsable asignado correctamente" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Quita un usuario como responsable de un proyecto.
    /// Solo el Administrador puede quitar responsables.
    /// </summary>
    [HttpDelete("{id}/responsables/{usuarioId}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> QuitarResponsable(int id, int usuarioId)
    {
        await _proyectoService.QuitarResponsableAsync(id, usuarioId);
        return Ok(new { mensaje = "Responsable quitado correctamente" });
    }
}