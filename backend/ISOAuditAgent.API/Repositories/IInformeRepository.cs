using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Interfaz del repositorio de informes.
/// Define que operaciones existen sin decir como se implementan.
/// </summary>
public interface IInformeRepository
{
    /// <summary>
    /// Devuelve todos los informes activos.
    /// </summary>
    Task<IReadOnlyList<Informe>> ObtenerTodosAsync();

    /// <summary>
    /// Devuelve los informes de una auditoria especifica.
    /// </summary>
    Task<IReadOnlyList<Informe>> ObtenerPorAuditoriaAsync(int auditoriaId);

    /// <summary>
    /// Busca un informe por su ID.
    /// Devuelve null si no existe.
    /// </summary>
    Task<Informe?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Crea un informe nuevo.
    /// </summary>
    Task<Informe> CrearAsync(Informe informe);

    /// <summary>
    /// Actualiza un informe existente.
    /// </summary>
    Task<Informe> ActualizarAsync(Informe informe);
}