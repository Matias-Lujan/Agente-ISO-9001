// ============================================================================
//  AuthService.cs — logica de autenticacion
// ----------------------------------------------------------------------------
//  Flujo del login:
//   1. Validar dominio del email (@bdtglobal.com.ar / @bdtglobal.com)
//   2. Buscar usuario en la BD
//   3. Verificar contrasena con BCrypt
//   4. Verificar que el usuario este activo
//   5. Generar JWT y devolverlo
//
//  Si CUALQUIER paso falla, devolvemos null. El endpoint traduce eso a 401.
//  No revelamos cual paso fallo (practica de seguridad).
// ============================================================================

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace ISOAuditAgent.API.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<Usuario?> ObtenerPerfilAsync(int usuarioId, CancellationToken ct);
}

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    // Dominios habilitados. Agregar nuevos aca si hace falta.
    private static readonly string[] DominiosPermitidos =
    {
        "@bdtglobal.com.ar",
        "@bdtglobal.com"
    };

    public AuthService(
        IUsuarioRepository usuarioRepo,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _usuarioRepo = usuarioRepo;
        _config = config;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var emailNormalizado = request.Email.Trim().ToLowerInvariant();

        _logger.LogInformation("Intento de login | Email={Email}", emailNormalizado);

        // Paso 1: validar dominio
        var dominioValido = DominiosPermitidos.Any(d => emailNormalizado.EndsWith(d));
        if (!dominioValido)
        {
            _logger.LogWarning("Login rechazado — dominio no habilitado: {Email}", emailNormalizado);
            return null;
        }

        // Paso 2: buscar usuario
        var usuario = await _usuarioRepo.ObtenerPorEmailOrNullAsync(emailNormalizado, ct);
        if (usuario is null)
        {
            _logger.LogWarning("Login fallido — email no encontrado: {Email}", emailNormalizado);
            return null;
        }

        // Paso 3: verificar contrasena con BCrypt
        var passwordCorrecta = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);
        if (!passwordCorrecta)
        {
            _logger.LogWarning("Login fallido — contrasena incorrecta: {Email}", emailNormalizado);
            return null;
        }

        // Paso 4: verificar que este activo
        if (!usuario.Activo)
        {
            _logger.LogWarning("Login fallido — usuario inactivo: {Email}", emailNormalizado);
            return null;
        }

        // Paso 5: generar JWT
        var (token, expiracion) = GenerarJwt(usuario);

        _logger.LogInformation(
            "Login exitoso | Email={Email} | Rol={Rol}",
            usuario.Email, usuario.Rol);

        return new LoginResponse(
            Token: token,
            Nombre: usuario.Nombre,
            Email: usuario.Email,
            Rol: usuario.Rol.ToString(),
            Expiracion: expiracion);
    }

    public async Task<Usuario?> ObtenerPerfilAsync(int usuarioId, CancellationToken ct)
    {
        return await _usuarioRepo.ObtenerPorIdOrNullAsync(usuarioId, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private (string Token, DateTime Expiracion) GenerarJwt(Usuario usuario)
    {
        var secretKey = _config["Jwt:SecretKey"]
            ?? throw new InvalidOperationException(
                "Jwt:SecretKey no configurada. Agregala en appsettings.Development.json " +
                "o como user-secret.");

        var horas = _config.GetValue<int?>("Jwt:ExpirationHours") ?? 8;
        var expiracion = DateTime.UtcNow.AddHours(horas);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name,           usuario.Nombre),
            new Claim(ClaimTypes.Email,          usuario.Email),
            new Claim(ClaimTypes.Role,           usuario.Rol.ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"]   ?? "ISOAuditAgent",
            audience:           _config["Jwt:Audience"] ?? "ISOAuditAgent",
            claims:             claims,
            expires:            expiracion,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiracion);
    }
}