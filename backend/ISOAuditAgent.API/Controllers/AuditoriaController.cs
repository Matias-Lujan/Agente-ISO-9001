using ISOAuditAgent.API.Agents.Orchestrator;
using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

[ApiController]
[Route("api/auditorias")]
[Authorize]
public class AuditoriaController : ControllerBase
{
    private readonly AuditoriaService _auditoriaService;
    private readonly IAuditoriaQueue _cola;
    private readonly IAuditoriaProgresoRepository _progresoRepo;
    private readonly ILogger<AuditoriaController> _logger;

    public AuditoriaController(
        AuditoriaService auditoriaService,
        IAuditoriaQueue cola,
        IAuditoriaProgresoRepository progresoRepo,
        ILogger<AuditoriaController> logger)
    {
        _auditoriaService = auditoriaService;
        _cola             = cola;
        _progresoRepo     = progresoRepo;
        _logger           = logger;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ObtenerTodas()
    {
        var auditorias = await _auditoriaService.ObtenerTodasAsync();
        return Ok(auditorias);
    }

    [HttpGet("proyecto/{proyectoId}")]
    public async Task<IActionResult> ObtenerPorProyecto(int proyectoId)
    {
        var auditorias = await _auditoriaService.ObtenerPorProyectoAsync(proyectoId);
        return Ok(auditorias);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var auditoria = await _auditoriaService.ObtenerPorIdAsync(id);
        if (auditoria == null)
            return NotFound(new { mensaje = $"Auditoría con ID {id} no encontrada" });
        return Ok(auditoria);
    }

    [HttpGet("{id}/progreso")]
    public async Task<IActionResult> ObtenerProgreso(int id, CancellationToken ct)
    {
        // Lee el progreso real por nodo que escribe el AuditoriaProgresoTracker.
        // Lista vacía si el worker todavía no inicializó las filas (el frontend
        // lo interpreta como los 4 nodos "Pendiente").
        var filas = await _progresoRepo.ObtenerPorAuditoriaAsync(id, ct);
        var progreso = filas.Select(p => new ProgresoNodoResponse(
            p.Nodo.ToString(),
            p.Estado.ToString(),
            p.FechaInicioUtc,
            p.FechaFinUtc));
        return Ok(progreso);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearAuditoriaRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (idClaim == null || !int.TryParse(idClaim, out var usuarioId))
            return Unauthorized();

        try
        {
            var auditoria = await _auditoriaService.CrearAsync(request, usuarioId);

            // Encolar para que el worker en background ejecute el workflow de agentes.
            await _cola.EncolarAsync(auditoria.Id);

            return CreatedAtAction(nameof(ObtenerPorId), new { id = auditoria.Id }, auditoria);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
    }

    [HttpPut("{id}/estado")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] string estado)
    {
        // Parseamos el string al enum EstadoAuditoria
        if (!Enum.TryParse<EstadoAuditoria>(estado, true, out var estadoEnum))
            return BadRequest(new { mensaje = $"Estado inválido: {estado}. Debe ser Completada o Fallida" });

        if (estadoEnum == EstadoAuditoria.EnCurso)
            return BadRequest(new { mensaje = "No se puede cambiar el estado a EnCurso manualmente" });

        var auditoria = await _auditoriaService.ActualizarEstadoAsync(id, estadoEnum);
        if (auditoria == null)
            return NotFound(new { mensaje = $"Auditoría con ID {id} no encontrada" });

        return Ok(auditoria);
    }
}