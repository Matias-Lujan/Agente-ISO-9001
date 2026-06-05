using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de usuarios usando Entity Framework + MySQL.
/// El DbContext es inyectado automaticamente — no hay que crearlo manualmente.
/// </summary>
public class UsuarioRepository : IUsuarioRepository
{
    private readonly ISOAuditAgentDbContext _db;
    private readonly ILogger<UsuarioRepository> _logger;

    public UsuarioRepository(ISOAuditAgentDbContext db, ILogger<UsuarioRepository> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<Usuario?> ObtenerPorEmailAsync(string email)
    {
        try
        {
            return await _db.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar usuario por email: {Email}", email);
            return null;
        }
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int id)
    {
        try
        {
            return await _db.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar usuario por ID: {Id}", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<Usuario>> ObtenerTodosAsync()
    {
        try
        {
            return await _db.Usuarios
                .OrderBy(u => u.Nombre)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los usuarios");
            return [];
        }
    }

    public async Task<Usuario> CrearAsync(Usuario usuario)
    {
        usuario.FechaCreacion = DateTime.UtcNow;
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        return usuario;
    }

    public async Task<Usuario> ActualizarAsync(Usuario usuario)
    {
        _db.Usuarios.Update(usuario);
        await _db.SaveChangesAsync();
        return usuario;
    }
}