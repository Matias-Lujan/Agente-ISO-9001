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
///   GET  /api/auth/usuarios          → solo Administrador
///   POST /api/auth/usuarios           → solo Administrador
///   PUT  /api/auth/usuarios/{id}      → solo Administrador
///   GET  /api/auth/me                 → cualquier usuario autenticado
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
        _logger      = logger;
    }

    // ── LOGIN ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inicio de sesion. Devuelve un JWT si las credenciales son correctas.
    /// Este endpoint es publico — no requiere token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous] // Permite el acceso sin token
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resultado = await _authService.LoginAsync(request);

        // Si el resultado es null, las credenciales son incorrectas
        // Devolvemos 401 (Unauthorized) sin decir si el error es el email o la contrasena
        // Esto es una practica de seguridad — no le damos pistas a un atacante
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
    [Authorize] // Requiere token JWT valido
    public async Task<IActionResult> ObtenerPerfil()
    {
        // El ID del usuario viene dentro del token JWT
        // No necesitamos consultarlo en la BD para saber quien es
        var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (idClaim == null || !int.TryParse(idClaim, out var id))
            return Unauthorized();

        var usuario = await _authService.ObtenerTodosAsync();
        var perfil  = usuario.FirstOrDefault(u => u.Id == id);

        if (perfil == null)
            return NotFound();

        return Ok(perfil);
    }

    // ── GESTION DE USUARIOS (solo Administrador) ──────────────────────────────

    /// <summary>
    /// Devuelve la lista de todos los usuarios.
    /// Solo el Administrador puede ver esta lista.
    /// </summary>
    [HttpGet("usuarios")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ObtenerUsuarios()
    {
        var usuarios = await _authService.ObtenerTodosAsync();
        return Ok(usuarios);
    }

    /// <summary>
    /// Crea un usuario nuevo.
    /// Solo el Administrador puede crear usuarios.
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

            // 201 Created — con la URL del recurso creado en el header Location
            return CreatedAtAction(nameof(ObtenerUsuarios), new { id = usuario.Id }, usuario);
        }
        catch (InvalidOperationException ex)
        {
            // Email ya registrado
            return Conflict(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            // Rol invalido
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    /// <summary>
    /// Modifica los datos de un usuario existente.
    /// Solo el Administrador puede modificar usuarios.
    /// </summary>
    [HttpPut("usuarios/{id}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ModificarUsuario(int id, [FromBody] ModificarUsuarioRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var usuario = await _authService.ModificarUsuarioAsync(id, request);

        if (usuario == null)
            return NotFound(new { mensaje = $"Usuario con ID {id} no encontrado" });

        return Ok(usuario);
    }

}