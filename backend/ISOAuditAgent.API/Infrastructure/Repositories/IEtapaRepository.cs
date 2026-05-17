using ISOAuditAgent.Infrastructure.Entities;

namespace ISOAuditAgent.Infrastructure.Repositories;

/// <summary>
/// Acceso de solo lectura a la tabla <c>etapa</c>.
/// </summary>
public interface IEtapaRepository
{
    /// <summary>
    /// Devuelve la etapa por su Id, o <c>null</c> si no existe.
    /// </summary>
    Task<Etapa?> GetByIdAsync(int etapaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista las etapas de un procedimiento, ordenadas por
    /// <see cref="Etapa.Orden"/>. Útil para comparar exigibilidad
    /// (etapas anteriores y actual = exigibles; etapas posteriores =
    /// pendientes de etapa futura).
    /// </summary>
    Task<IReadOnlyList<Etapa>> ListarPorProcedimientoAsync(
        int procedimientoId,
        CancellationToken cancellationToken = default);
}
