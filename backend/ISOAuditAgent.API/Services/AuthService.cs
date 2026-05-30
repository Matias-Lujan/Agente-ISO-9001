using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Servicio de autenticacion y gestion de usuarios.
/// Actualizado para usar el enum RolUsuario en lugar de strings.
/// </summary>
public class AuthService
{
    private readonly IUsuarioRepository _repo;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUsuarioRepository repo,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _repo   = repo;
        _config = config;
        _logger = logger;
    }

    // ── LOGIN ─────────────────────────────────────────────────────────────────

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Intento de login | Email={Email}", request.Email);

        var usuario = await _repo.ObtenerPorEmailAsync(request.Email);
        if (usuario == null)
        {
            _logger.LogWarning("Login fallido — email no encontrado: {Email}", request.Email);
            return null;
        }

        var passwordCorrecta = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);
        if (!passwordCorrecta)
        {
            _logger.LogWarning("Login fallido — contrasena incorrecta: {Email}", request.Email);
            return null;
        }

        if (!usuario.Activo)
        {
            _logger.LogWarning("Login fallido — usuario inactivo: {Email}", request.Email);
            return null;
        }

        var token      = GenerarJwt(usuario);
        var expiracion = DateTime.UtcNow.AddHours(8);

        _logger.LogInformation("Login exitoso | Email={Email} | Rol={Rol}", usuario.Email, usuario.Rol);

        return new LoginResponse(
            token,
            usuario.Nombre,
            usuario.Email,
            // Convertimos el enum a string para el DTO
            usuario.Rol.ToString(),
            expiracion);
    }

    // ── GESTION DE USUARIOS ───────────────────────────────────────────────────

    public async Task<UsuarioResponse> CrearUsuarioAsync(CrearUsuarioRequest request)
    {
        var existente = await _repo.ObtenerPorEmailAsync(request.Email);
        if (existente != null)
            throw new InvalidOperationException($"Ya existe un usuario con el email {request.Email}");

        // Parseamos el string del rol al enum RolUsuario
        if (!Enum.TryParse<RolUsuario>(request.Rol, out var rolEnum))
            throw new ArgumentException($"Rol invalido: {request.Rol}. Debe ser Administrador, Operador o Auditor");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var usuario = new Usuario
        {
            Nombre       = request.Nombre,
            Email        = request.Email,
            PasswordHash = passwordHash,
            Rol          = rolEnum,
            Activo       = true
        };

        var creado = await _repo.CrearAsync(usuario);

        _logger.LogInformation("Usuario creado | Email={Email} | Rol={Rol}", creado.Email, creado.Rol);

        return MapearAResponse(creado);
    }

    public async Task<IReadOnlyList<UsuarioResponse>> ObtenerTodosAsync()
    {
        var usuarios = await _repo.ObtenerTodosAsync();
        return usuarios.Select(MapearAResponse).ToList();
    }

    public async Task<UsuarioResponse?> ModificarUsuarioAsync(int id, ModificarUsuarioRequest request)
    {
        var usuario = await _repo.ObtenerPorIdAsync(id);
        if (usuario == null) return null;

        if (request.Nombre != null) usuario.Nombre = request.Nombre;
        if (request.Email  != null) usuario.Email  = request.Email;
        if (request.Activo != null) usuario.Activo = request.Activo.Value;

        // Si viene un nuevo rol, lo parseamos al enum
        if (request.Rol != null)
        {
            if (!Enum.TryParse<RolUsuario>(request.Rol, out var rolEnum))
                throw new ArgumentException($"Rol invalido: {request.Rol}");
            usuario.Rol = rolEnum;
        }

        var actualizado = await _repo.ActualizarAsync(usuario);
        return MapearAResponse(actualizado);
    }

    // ── Generacion del JWT ────────────────────────────────────────────────────

    private string GenerarJwt(Usuario usuario)
    {
        var secretKey = _config["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey no configurada");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name,           usuario.Nombre),
            new Claim(ClaimTypes.Email,          usuario.Email),
            // El rol va como string en el JWT — usamos ToString() del enum
            new Claim(ClaimTypes.Role,           usuario.Rol.ToString())
        };

        var token = new JwtSecurityToken(
            issuer:   _config["Jwt:Issuer"]   ?? "ISOAuditAgent",
            audience: _config["Jwt:Audience"] ?? "ISOAuditAgent",
            claims:   claims,
            expires:  DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UsuarioResponse MapearAResponse(Usuario u) => new(
        u.Id,
        u.Nombre,
        u.Email,
        // Convertimos el enum a string para el DTO
        u.Rol.ToString(),
        u.Activo,
        u.FechaCreacion);
}