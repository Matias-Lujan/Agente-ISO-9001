using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Interfaz del repositorio de usuarios.
///
/// Una interfaz es un contrato que define QUE operaciones existen
/// sin decir COMO se implementan.
/// </summary>
public interface IUsuarioRepository
{
    /// <summary>
    /// Busca un usuario por su email.
    /// Se usa en el login para verificar si el usuario existe.
    /// Devuelve null si no existe.
    /// </summary>
    Task<Usuario?> ObtenerPorEmailAsync(string email);

    /// <summary>
    /// Busca un usuario por su ID.
    /// Se usa para obtener los datos de un usuario especifico.
    /// Devuelve null si no existe.
    /// </summary>
    Task<Usuario?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Devuelve todos los usuarios del sistema.
    /// Solo el Administrador puede ver esta lista.
    /// </summary>
    Task<IReadOnlyList<Usuario>> ObtenerTodosAsync();

    /// <summary>
    /// Crea un usuario nuevo en la base de datos.
    /// Devuelve el usuario creado con su ID asignado por la BD.
    /// </summary>
    Task<Usuario> CrearAsync(Usuario usuario);

    /// <summary>
    /// Actualiza los datos de un usuario existente.
    /// Devuelve el usuario actualizado.
    /// </summary>
    Task<Usuario> ActualizarAsync(Usuario usuario);
}