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
///
/// Mejoras agregadas:
///  - Validacion de dominio @bdtglobal.com.ar en login y al crear usuarios
///  - Reglas de auto-bloqueo (admin no se desactiva ni se cambia el rol a si mismo)
///  - Metodo para resetear password (admin cambia pass de otro usuario)
///  - ObtenerPorIdAsync expuesto (usado por GET /api/auth/me)
/// </summary>
public class AuthService
{
    private readonly IUsuarioRepository _repo;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    // Dominios habilitados. Agregar nuevos aca si hace falta.
    private static readonly string[] DominiosPermitidos =
    {
        "@bdtglobal.com.ar",
        "@bdtglobal.com"
    };

    private const int PASSWORD_MIN_LENGTH = 8;

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
        var emailNormalizado = (request.Email ?? "").Trim().ToLowerInvariant();
        _logger.LogInformation("Intento de login | Email={Email}", emailNormalizado);

        // Paso 1: validar dominio
        if (!DominioValido(emailNormalizado))
        {
            _logger.LogWarning("Login rechazado — dominio no habilitado: {Email}", emailNormalizado);
            return null;
        }

        // Paso 2: buscar usuario
        var usuario = await _repo.ObtenerPorEmailAsync(emailNormalizado);
        if (usuario == null)
        {
            _logger.LogWarning("Login fallido — email no encontrado: {Email}", emailNormalizado);
            return null;
        }

        // Paso 3: verificar contrasena
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
        var token      = GenerarJwt(usuario);
        var expiracion = DateTime.UtcNow.AddHours(8);

        _logger.LogInformation("Login exitoso | Email={Email} | Rol={Rol}", usuario.Email, usuario.Rol);

        return new LoginResponse(
            token,
            usuario.Nombre,
            usuario.Email,
            usuario.Rol.ToString(),
            expiracion);
    }

    // ── GESTION DE USUARIOS ───────────────────────────────────────────────────

    public async Task<UsuarioResponse> CrearUsuarioAsync(CrearUsuarioRequest request)
    {
        // Validacion: nombre
        var nombre = (request.Nombre ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio");

        // Validacion: email
        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email es obligatorio");

        if (!DominioValido(email))
            throw new ArgumentException("Solo se permiten cuentas @bdtglobal.com.ar");

        // Validacion: que el email no exista
        var existente = await _repo.ObtenerPorEmailAsync(email);
        if (existente != null)
            throw new InvalidOperationException($"Ya existe un usuario con el email {email}");

        // Validacion: password
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("La contrasena es obligatoria");

        if (request.Password.Length < PASSWORD_MIN_LENGTH)
            throw new ArgumentException($"La contrasena debe tener al menos {PASSWORD_MIN_LENGTH} caracteres");

        // Validacion: rol
        if (!Enum.TryParse<RolUsuario>(request.Rol, out var rolEnum))
            throw new ArgumentException($"Rol invalido: {request.Rol}. Debe ser Administrador, Operador o Auditor");

        // Crear usuario
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11);

        var usuario = new Usuario
        {
            Nombre       = nombre,
            Email        = email,
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

    /// <summary>
    /// Obtiene un usuario por su ID. Usado por GET /api/auth/me y por el ABM.
    /// </summary>
    public async Task<UsuarioResponse?> ObtenerPorIdAsync(int id)
    {
        var usuario = await _repo.ObtenerPorIdAsync(id);
        return usuario == null ? null : MapearAResponse(usuario);
    }

    /// <summary>
    /// Modifica un usuario. Aplica reglas de auto-bloqueo si el admin se
    /// esta modificando a si mismo (no puede desactivarse ni cambiarse el rol).
    /// </summary>
    /// <param name="id">ID del usuario a modificar</param>
    /// <param name="adminIdActual">ID del admin que esta haciendo la modificacion</param>
    /// <param name="request">Datos a modificar</param>
    public async Task<UsuarioResponse?> ModificarUsuarioAsync(
        int id,
        int adminIdActual,
        ModificarUsuarioRequest request)
    {
        var usuario = await _repo.ObtenerPorIdAsync(id);
        if (usuario == null) return null;

        var esSiMismo = id == adminIdActual;

        // Validar email si viene
        if (request.Email != null)
        {
            var emailNorm = request.Email.Trim().ToLowerInvariant();
            if (!DominioValido(emailNorm))
                throw new ArgumentException("Solo se permiten cuentas @bdtglobal.com.ar");

            // Verificar que el email no este usado por OTRO usuario
            if (emailNorm != usuario.Email)
            {
                var existente = await _repo.ObtenerPorEmailAsync(emailNorm);
                if (existente != null && existente.Id != id)
                    throw new InvalidOperationException($"Ya existe otro usuario con el email {emailNorm}");
            }

            usuario.Email = emailNorm;
        }

        // Modificar nombre
        if (request.Nombre != null)
        {
            var nombre = request.Nombre.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre no puede estar vacio");
            usuario.Nombre = nombre;
        }

        // Modificar activo (con auto-bloqueo)
        if (request.Activo != null)
        {
            if (esSiMismo && !request.Activo.Value)
                throw new InvalidOperationException("No podes desactivar tu propia cuenta");
            usuario.Activo = request.Activo.Value;
        }

        // Modificar rol (con auto-bloqueo)
        if (request.Rol != null)
        {
            if (!Enum.TryParse<RolUsuario>(request.Rol, out var rolEnum))
                throw new ArgumentException($"Rol invalido: {request.Rol}");

            if (esSiMismo && rolEnum != usuario.Rol)
                throw new InvalidOperationException("No podes cambiarte el rol a vos mismo");

            usuario.Rol = rolEnum;
        }

        var actualizado = await _repo.ActualizarAsync(usuario);

        _logger.LogInformation(
            "Usuario modificado | Id={Id} | ModificadoPor={Admin}",
            id, adminIdActual);

        return MapearAResponse(actualizado);
    }

    /// <summary>
    /// El admin resetea la contrasena de otro usuario. No pide la pass anterior
    /// porque el admin no la conoce.
    /// </summary>
    public async Task<bool> ResetearPasswordAsync(int id, ResetearPasswordRequest request)
    {
        var usuario = await _repo.ObtenerPorIdAsync(id);
        if (usuario == null) return false;

        if (string.IsNullOrWhiteSpace(request.PasswordNueva))
            throw new ArgumentException("La nueva contrasena es obligatoria");

        if (request.PasswordNueva.Length < PASSWORD_MIN_LENGTH)
            throw new ArgumentException($"La contrasena debe tener al menos {PASSWORD_MIN_LENGTH} caracteres");

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordNueva, workFactor: 11);
        await _repo.ActualizarAsync(usuario);

        _logger.LogInformation("Password reseteada | UsuarioId={Id}", id);
        return true;
    }

    // ── HELPERS PRIVADOS ──────────────────────────────────────────────────────

    private static bool DominioValido(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return DominiosPermitidos.Any(d => email.EndsWith(d));
    }

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
        u.Rol.ToString(),
        u.Activo,
        u.FechaCreacion);
}