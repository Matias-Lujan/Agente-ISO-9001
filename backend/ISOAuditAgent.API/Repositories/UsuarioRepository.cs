// ============================================================================
//  UsuarioRepository.cs — acceso a la tabla usuario
// ----------------------------------------------------------------------------
//
//   - El repo SOLO accede a la BD via ISOAuditAgentDbContext. La logica del
//     login (validacion de dominio, BCrypt, generacion de JWT) vive en
//     AuthService.
// ============================================================================

using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Acceso de lectura a la entidad Usuario. La unica escritura en MVP es la
/// creacion de usuarios (post-login, no se incluye en esta iteracion).
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>
    /// Busca un usuario por email exacto. El email tiene indice unico
    /// (ver ISOAuditAgentDbContext), asi que FirstOrDefault es seguro.
    /// </summary>
    Task<Usuario?> ObtenerPorEmailOrNullAsync(string email, CancellationToken ct);

    /// <summary>
    /// Busca un usuario por id. Lo usa /api/auth/me para devolver el perfil
    /// a partir del claim NameIdentifier del JWT.
    /// </summary>
    Task<Usuario?> ObtenerPorIdOrNullAsync(int id, CancellationToken ct);
}

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ISOAuditAgentDbContext _db;

    public UsuarioRepository(ISOAuditAgentDbContext db)
    {
        _db = db;
    }

    public async Task<Usuario?> ObtenerPorEmailOrNullAsync(string email, CancellationToken ct)
    {
        return await _db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task<Usuario?> ObtenerPorIdOrNullAsync(int id, CancellationToken ct)
    {
        return await _db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }
}