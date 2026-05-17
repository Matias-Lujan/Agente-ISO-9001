using ISOAuditAgent.API.Models;
using Supabase;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de usuarios usando Supabase.
///
/// Esta clase es la unica que sabe como hablar con Supabase.
/// El resto del sistema solo conoce la interfaz IUsuarioRepository.
/// Si el cliente decide cambiar de base de datos, solo hay que
/// crear una nueva clase como esta y cambiar una linea en Program.cs.
/// </summary>
public class UsuarioRepository : IUsuarioRepository
{
    private readonly Client _supabase;
    private readonly ILogger<UsuarioRepository> _logger;

    public UsuarioRepository(Client supabase, ILogger<UsuarioRepository> logger)
    {
        _supabase = supabase;
        _logger   = logger;
    }

    /// <summary>
    /// Busca un usuario por email.
    /// Se usa en el login para verificar si el usuario existe.
    /// </summary>
    public async Task<Usuario?> ObtenerPorEmailAsync(string email)
    {
        try
        {
            var response = await _supabase
                .From<UsuarioSupabase>()
                .Where(u => u.Email == email)
                .Single();

            return response == null ? null : MapearAUsuario(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar usuario por email: {Email}", email);
            return null;
        }
    }

    /// <summary>
    /// Busca un usuario por ID.
    /// </summary>
    public async Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var response = await _supabase
                .From<UsuarioSupabase>()
                .Where(u => u.Id == id)
                .Single();

            return response == null ? null : MapearAUsuario(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar usuario por ID: {Id}", id);
            return null;
        }
    }

    /// <summary>
    /// Devuelve todos los usuarios del sistema.
    /// </summary>
    public async Task<IReadOnlyList<Usuario>> ObtenerTodosAsync()
    {
        try
        {
            var response = await _supabase
                .From<UsuarioSupabase>()
                .Get();

            return response.Models
                .Select(MapearAUsuario)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los usuarios");
            return [];
        }
    }

    /// <summary>
    /// Crea un usuario nuevo en Supabase.
    /// </summary>
    public async Task<Usuario> CrearAsync(Usuario usuario)
    {
        var supabaseUsuario = new UsuarioSupabase
        {
            Nombre       = usuario.Nombre,
            Email        = usuario.Email,
            PasswordHash = usuario.PasswordHash,
            Rol          = usuario.Rol,
            Activo       = usuario.Activo
        };

        var response = await _supabase
            .From<UsuarioSupabase>()
            .Insert(supabaseUsuario);

        return MapearAUsuario(response.Models.First());
    }

    /// <summary>
    /// Actualiza los datos de un usuario existente.
    /// </summary>
    public async Task<Usuario> ActualizarAsync(Usuario usuario)
    {
        var supabaseUsuario = new UsuarioSupabase
        {
            Id           = usuario.Id,
            Nombre       = usuario.Nombre,
            Email        = usuario.Email,
            PasswordHash = usuario.PasswordHash,
            Rol          = usuario.Rol,
            Activo       = usuario.Activo
        };

        var response = await _supabase
            .From<UsuarioSupabase>()
            .Where(u => u.Id == usuario.Id)
            .Update(supabaseUsuario);

        return MapearAUsuario(response.Models.First());
    }

    // ── Mapeo entre el modelo de Supabase y el modelo del dominio ────────────
    // Supabase necesita su propio modelo con atributos especiales.
    // Esta funcion convierte de uno al otro para que el resto del sistema
    // solo conozca el modelo Usuario, no el de Supabase.

    private static Usuario MapearAUsuario(UsuarioSupabase u) => new()
    {
        Id           = u.Id,
        Nombre       = u.Nombre,
        Email        = u.Email,
        PasswordHash = u.PasswordHash,
        Rol          = u.Rol,
        Activo       = u.Activo,
        FechaCreacion = u.FechaCreacion
    };
}

/// <summary>
/// Modelo especifico para Supabase.
/// Supabase necesita que los nombres de las propiedades coincidan
/// exactamente con las columnas de la tabla en la base de datos.
/// Por eso existe este modelo separado del Usuario del dominio.
/// </summary>
[Supabase.Postgrest.Attributes.Table("usuario")]
public class UsuarioSupabase : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id")]
    public int Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Supabase.Postgrest.Attributes.Column("email")]
    public string Email { get; set; } = string.Empty;

    [Supabase.Postgrest.Attributes.Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Supabase.Postgrest.Attributes.Column("rol")]
    public string Rol { get; set; } = string.Empty;

    [Supabase.Postgrest.Attributes.Column("activo")]
    public bool Activo { get; set; } = true;

    [Supabase.Postgrest.Attributes.Column("fecha_creacion")]
    public DateTime FechaCreacion { get; set; }
}