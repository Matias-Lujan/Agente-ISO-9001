using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Interfaz del repositorio de proyectos.
/// Define que operaciones existen con proyectos
/// sin decir como se implementan.
/// </summary>
public interface IProyectoRepository
{
    /// <summary>
    /// Devuelve todos los proyectos activos.
    /// </summary>
    Task<IReadOnlyList<Proyecto>> ObtenerTodosAsync();

    /// <summary>
    /// Busca un proyecto por su ID.
    /// Devuelve null si no existe.
    /// </summary>
    Task<Proyecto?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Devuelve los proyectos asignados a un usuario especifico.
    /// Se usa para que cada usuario vea solo sus proyectos.
    /// </summary>
    Task<IReadOnlyList<Proyecto>> ObtenerPorUsuarioAsync(int usuarioId);

    /// <summary>
    /// Crea un proyecto nuevo.
    /// </summary>
    Task<Proyecto> CrearAsync(Proyecto proyecto);

    /// <summary>
    /// Actualiza los datos de un proyecto existente.
    /// </summary>
    Task<Proyecto> ActualizarAsync(Proyecto proyecto);

    /// <summary>
    /// Asigna un usuario a un proyecto.
    /// </summary>
    Task AsignarResponsableAsync(int proyectoId, int usuarioId);

    /// <summary>
    /// Quita un usuario de un proyecto.
    /// </summary>
    Task QuitarResponsableAsync(int proyectoId, int usuarioId);

    /// <summary>
    /// Devuelve los usuarios asignados a un proyecto.
    /// </summary>
    Task<IReadOnlyList<int>> ObtenerResponsablesAsync(int proyectoId);

    // ── Usados por el workflow del orchestrator (portado de dev) ──────────────

    /// <summary>
    /// Trae el proyecto por id (con integraciones Drive/Trello/Clockify). Null si no existe.
    /// </summary>
    Task<Proyecto?> ObtenerPorIdOrNullAsync(int proyectoId, CancellationToken ct);

    /// <summary>
    /// Lista los proyectos activos, ordenados por nombre.
    /// </summary>
    Task<IReadOnlyList<Proyecto>> ObtenerActivosAsync(CancellationToken ct);
}