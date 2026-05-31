using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Interfaz del repositorio de hallazgos.
/// Los hallazgos los insertan los agentes (FindingsClassification).
/// El backend solo los lee — no los crea ni modifica.
/// </summary>
public interface IHallazgoRepository
{
    /// <summary>
    /// Devuelve todos los hallazgos de una auditoria.
    /// </summary>
    Task<IReadOnlyList<Hallazgo>> ObtenerPorAuditoriaAsync(int auditoriaId);

    /// <summary>
    /// Busca un hallazgo por su ID.
    /// </summary>
    Task<Hallazgo?> ObtenerPorIdAsync(int id);
}