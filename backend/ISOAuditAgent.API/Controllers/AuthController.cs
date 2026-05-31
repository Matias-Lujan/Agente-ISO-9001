using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Controller de autenticacion y gestion de usuarios.
///
/// Endpoints publicos (no requieren token):
///   POST /api/auth/login
///
/// Endpoints protegidos (requieren token JWT):
///   GET  /api/auth/me                      → cualquier usuario autenticado
///
/// Endpoints solo para Administrador:
///   GET    /api/auth/usuarios               → lista de todos los usuarios
///   GET    /api/auth/usuarios/{id}          → detalle de un usuario
///   POST   /api/auth/usuarios               → crear usuario
///   PUT    /api/auth/usuarios/{id}          → modificar usuario
///   PUT    /api/auth/usuarios/{id}/password → resetear contrasena
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // ── LOGIN ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicio de sesion. Devuelve un JWT si las credenciales son correctas.
    /// Endpoint publico — no requiere token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resultado = await _authService.LoginAsync(request);

        // 401 sin distinguir cual paso fallo (practica de seguridad)
        if (resultado == null)
            return Unauthorized(new { mensaje = "Credenciales incorrectas" });

        return Ok(resultado);
    }

    // ── PERFIL DEL USUARIO AUTENTICADO ────────────────────────────────────────

    /// <summary>
    /// Devuelve los datos del usuario que esta logueado.
    /// Cualquier usuario autenticado puede llamar a este endpoint.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> ObtenerPerfil()
    {
        var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (idClaim == null || !int.TryParse(idClaim, out var id))
            return Unauthorized();

        var perfil = await _authService.ObtenerPorIdAsync(id);
        if (perfil == null)
            return NotFound();

        return Ok(perfil);
    }

    // ── GESTION DE USUARIOS (solo Administrador) ──────────────────────────────

    /// <summary>
    /// Devuelve la lista de todos los usuarios.
    /// </summary>
    [HttpGet("usuarios")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ObtenerUsuarios()
    {
        _logger.LogInformation("Solicitud recibida en /api/auth/usuarios");
        Console.WriteLine("Solicitud recibida en /api/auth/usuarios");

        var usuarios = await _authService.ObtenerTodosAsync();
        return Ok(usuarios);
    }

    /// <summary>
    /// Devuelve el detalle de un usuario por su ID.
    /// </summary>
    [HttpGet("usuarios/{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ObtenerUsuario(int id)
    {
        if (id <= 0)
            return BadRequest(new { mensaje = "id debe ser > 0" });

        var usuario = await _authService.ObtenerPorIdAsync(id);
        if (usuario == null)
            return NotFound(new { mensaje = $"Usuario {id} no encontrado" });

        return Ok(usuario);
    }

    /// <summary>
    /// Crea un usuario nuevo.
    /// </summary>
    [HttpPost("usuarios")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var usuario = await _authService.CrearUsuarioAsync(request);
            return CreatedAtAction(nameof(ObtenerUsuario), new { id = usuario.Id }, usuario);
        }
        catch (InvalidOperationException ex)
        {
            // Email ya registrado
            return Conflict(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            // Validaciones (dominio, rol, password, etc.)
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Modifica los datos de un usuario existente.
    /// Aplica reglas de auto-bloqueo: el admin no puede desactivarse ni
    /// cambiarse el rol a si mismo.
    /// </summary>
    [HttpPut("usuarios/{id:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ModificarUsuario(int id, [FromBody] ModificarUsuarioRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Sacamos el ID del admin que esta haciendo la modificacion del JWT
        var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (adminIdClaim == null || !int.TryParse(adminIdClaim, out var adminId))
            return Unauthorized();

        try
        {
            var usuario = await _authService.ModificarUsuarioAsync(id, adminId, request);

            if (usuario == null)
                return NotFound(new { mensaje = $"Usuario con ID {id} no encontrado" });

            return Ok(usuario);
        }
        catch (InvalidOperationException ex)
        {
            // Email duplicado o auto-bloqueo
            return Conflict(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            // Validaciones (rol invalido, etc.)
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// El administrador resetea la contrasena de otro usuario.
    /// No pide la pass anterior porque el admin no la conoce.
    /// </summary>
    [HttpPut("usuarios/{id:int}/password")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ResetearPassword(int id, [FromBody] ResetearPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var ok = await _authService.ResetearPasswordAsync(id, request);
            if (!ok)
                return NotFound(new { mensaje = $"Usuario con ID {id} no encontrado" });

            return NoContent(); // 204 — cambio aplicado sin body
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}
