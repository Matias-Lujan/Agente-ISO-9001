using ISOAuditAgent.Infrastructure.Entities;

namespace ISOAuditAgent.Infrastructure.Repositories;

/// <summary>
/// Acceso de solo lectura a la tabla <c>proyecto</c>.
/// </summary>
public interface IProyectoRepository
{
    /// <summary>
    /// Devuelve el proyecto por su Id, o <c>null</c> si no existe o está
    /// inactivo. Trae el <see cref="Procedimiento"/> asociado eager.
    /// </summary>
    Task<Proyecto?> GetByIdAsync(int proyectoId, CancellationToken cancellationToken = default);
}
