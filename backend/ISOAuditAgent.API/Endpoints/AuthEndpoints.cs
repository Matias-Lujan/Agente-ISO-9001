// ============================================================================
//  AuthEndpoints.cs — endpoints REST de autenticacion
// ----------------------------------------------------------------------------
//  Sigue el patron de AuditoriaEndpoints.cs y MetadataEndpoints.cs:
//   - Extension method sobre WebApplication
//   - Se invoca desde Program.cs con app.MapAuthEndpoints()
//
//  Endpoints:
//    POST /api/auth/login    — Publico. Devuelve JWT.
//    GET  /api/auth/me       — Protegido. Devuelve perfil del usuario logueado.
//
//  En el futuro:
//    GET  /api/auth/usuarios       — solo Administrador
//    POST /api/auth/usuarios       — solo Administrador (crea con BCrypt hash)
//    PUT  /api/auth/usuarios/{id}  — solo Administrador
// ============================================================================

using System.Security.Claims;
using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Services;

namespace ISOAuditAgent.API.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // ── POST /api/auth/login — publico ────────────────────────────────
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest("El email es obligatorio.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest("La contrasena es obligatoria.");

            var resultado = await authService.LoginAsync(request, ct);

            if (resultado is null)
            {
                // 401 sin distinguir cual paso fallo (seguridad)
                return Results.Json(
                    "Credenciales incorrectas.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(resultado);
        })
        .AllowAnonymous();

        // ── GET /api/auth/me — protegido ──────────────────────────────────
        app.MapGet("/api/auth/me", async (
            ClaimsPrincipal user,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idClaim, out var usuarioId))
                return Results.Unauthorized();

            var usuario = await authService.ObtenerPerfilAsync(usuarioId, ct);

            if (usuario is null)
                return Results.NotFound("Usuario no encontrado.");

            // No devolvemos PasswordHash — buena practica nunca exponerlo
            return Results.Ok(new
            {
                id = usuario.Id,
                nombre = usuario.Nombre,
                email = usuario.Email,
                rol = usuario.Rol.ToString(),
                activo = usuario.Activo,
                fechaCreacion = usuario.FechaCreacion
            });
        })
        .RequireAuthorization();

        return app;
    }
}