using ISOAuditAgent.Infrastructure.Entities;

namespace ISOAuditAgent.Infrastructure.Repositories;

/// <summary>
/// Acceso de solo lectura a la tabla <c>artefacto_esperado</c>.
/// </summary>
public interface IArtefactoEsperadoRepository
{
    /// <summary>
    /// Lista todos los artefactos esperados de un procedimiento, con su
    /// <see cref="ArtefactoEsperado.Etapa"/> cargada eager (incluye
    /// <see cref="Etapa.Orden"/>) y ordenados por
    /// <c>Etapa.Orden</c>, luego por <c>Codigo</c>.
    /// </summary>
    /// <remarks>
    /// El agente decide la <c>Exigibilidad</c> comparando
    /// <c>Etapa.Orden</c> de cada artefacto contra el orden de la
    /// <c>EtapaActual</c> del proyecto. La clasificación es lógica de
    /// dominio del agente, no del repositorio.
    /// </remarks>
    Task<IReadOnlyList<ArtefactoEsperado>> ListarPorProcedimientoAsync(
        int procedimientoId,
        CancellationToken cancellationToken = default);
}
