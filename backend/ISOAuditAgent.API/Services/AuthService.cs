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
/// Se encarga de:
/// 1. Verificar credenciales en el login
/// 2. Generar el JWT cuando el login es exitoso
/// 3. Crear, modificar y listar usuarios (solo Administrador)
///
/// No sabe nada de base de datos — le delega eso al IUsuarioRepository.
/// No sabe nada de HTTP — eso lo maneja el Controller.
/// Su unica responsabilidad es la logica de autenticacion.
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

    /// <summary>
    /// Verifica las credenciales del usuario y devuelve un JWT si son correctas.
    ///
    /// Pasos:
    /// 1. Busca el usuario por email
    /// 2. Verifica que la contrasena sea correcta
    /// 3. Verifica que el usuario este activo
    /// 4. Genera y devuelve el JWT
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Intento de login | Email={Email}", request.Email);

        // Paso 1: buscar el usuario por email
        var usuario = await _repo.ObtenerPorEmailAsync(request.Email);
        if (usuario == null)
        {
            _logger.LogWarning("Login fallido — email no encontrado: {Email}", request.Email);
            return null;
        }

        // Paso 2: verificar la contrasena
        // BCrypt compara la contrasena ingresada con el hash guardado en la BD
        // Si no coinciden, devolvemos null (no decimos si el error es el email o la contrasena
        // por seguridad — asi un atacante no puede saber cual de los dos esta mal)
        var passwordCorrecta = BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash);
        if (!passwordCorrecta)
        {
            _logger.LogWarning("Login fallido — contrasena incorrecta: {Email}", request.Email);
            return null;
        }

        // Paso 3: verificar que el usuario este activo
        if (!usuario.Activo)
        {
            _logger.LogWarning("Login fallido — usuario inactivo: {Email}", request.Email);
            return null;
        }

        // Paso 4: generar el JWT y devolverlo
        var token      = GenerarJwt(usuario);
        var expiracion = DateTime.UtcNow.AddHours(8);

        _logger.LogInformation("Login exitoso | Email={Email} | Rol={Rol}", usuario.Email, usuario.Rol);

        return new LoginResponse(
            token,
            usuario.Nombre,
            usuario.Email,
            usuario.Rol,
            expiracion);
    }

    // ── GESTION DE USUARIOS ───────────────────────────────────────────────────

    /// <summary>
    /// Crea un usuario nuevo. Solo puede llamar a este metodo el Administrador.
    /// La validacion del rol se hace en el Controller con [Authorize(Roles = "Administrador")].
    /// </summary>
    public async Task<UsuarioResponse> CrearUsuarioAsync(CrearUsuarioRequest request)
    {
        // Verificar que el email no este ya registrado
        var existente = await _repo.ObtenerPorEmailAsync(request.Email);
        if (existente != null)
            throw new InvalidOperationException($"Ya existe un usuario con el email {request.Email}");

        // Validar que el rol sea valido
        if (request.Rol != Roles.Administrador &&
            request.Rol != Roles.Operador &&
            request.Rol != Roles.Auditor)
            throw new ArgumentException($"Rol invalido: {request.Rol}");

        // Hashear la contrasena antes de guardarla
        // NUNCA guardamos la contrasena en texto plano
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var usuario = new Usuario
        {
            Nombre       = request.Nombre,
            Email        = request.Email,
            PasswordHash = passwordHash,
            Rol          = request.Rol,
            Activo       = true
        };

        var creado = await _repo.CrearAsync(usuario);

        _logger.LogInformation(
            "Usuario creado | Email={Email} | Rol={Rol}", creado.Email, creado.Rol);

        return MapearAResponse(creado);
    }

    /// <summary>
    /// Devuelve todos los usuarios. Solo el Administrador puede ver esta lista.
    /// </summary>
    public async Task<IReadOnlyList<UsuarioResponse>> ObtenerTodosAsync()
    {
        var usuarios = await _repo.ObtenerTodosAsync();
        return usuarios.Select(MapearAResponse).ToList();
    }

    /// <summary>
    /// Modifica los datos de un usuario existente.
    /// Solo actualiza los campos que vienen con valor (los null se ignoran).
    /// </summary>
    public async Task<UsuarioResponse?> ModificarUsuarioAsync(int id, ModificarUsuarioRequest request)
    {
        var usuario = await _repo.ObtenerPorIdAsync(id);
        if (usuario == null)
            return null;

        // Solo actualizamos los campos que vienen con valor
        if (request.Nombre != null) usuario.Nombre = request.Nombre;
        if (request.Email  != null) usuario.Email  = request.Email;
        if (request.Rol    != null) usuario.Rol    = request.Rol;
        if (request.Activo != null) usuario.Activo = request.Activo.Value;

        var actualizado = await _repo.ActualizarAsync(usuario);
        return MapearAResponse(actualizado);
    }

    // ── Generacion del JWT ────────────────────────────────────────────────────

    /// <summary>
    /// Genera un JWT (JSON Web Token) para el usuario autenticado.
    ///
    /// El JWT contiene:
    /// - Claims (datos del usuario): nombre, email, rol
    /// - Fecha de expiracion: 8 horas
    /// - Firma digital: para verificar que no fue modificado
    ///
    /// El frontend lo guarda y lo manda en el header de cada request:
    /// Authorization: Bearer eyJhbGci...
    /// </summary>
    private string GenerarJwt(Usuario usuario)
    {
        // La clave secreta con la que firmamos el token
        // Vive en appsettings.Development.json → Jwt:SecretKey
        var secretKey = _config["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey no configurada");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims — datos del usuario que viajan dentro del token
        // El frontend puede leerlos sin necesitar consultar la BD
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name,           usuario.Nombre),
            new Claim(ClaimTypes.Email,          usuario.Email),
            new Claim(ClaimTypes.Role,           usuario.Rol)
        };

        var token = new JwtSecurityToken(
            issuer:   _config["Jwt:Issuer"]   ?? "ISOAuditAgent",
            audience: _config["Jwt:Audience"] ?? "ISOAuditAgent",
            claims:   claims,
            expires:  DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static UsuarioResponse MapearAResponse(Usuario u) => new(
        u.Id,
        u.Nombre,
        u.Email,
        u.Rol,
        u.Activo,
        u.FechaCreacion);
}