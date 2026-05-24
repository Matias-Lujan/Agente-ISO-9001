using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Interfaz del repositorio de reglas de validacion.
/// Define que operaciones existen sin decir como se implementan.
/// Cumple con RF-04 del ERS: reglas configurables por proceso.
/// </summary>
public interface IReglaValidacionRepository
{
    /// <summary>
    /// Devuelve todas las reglas activas de un procedimiento.
    /// </summary>
    Task<IReadOnlyList<ReglaValidacion>> ObtenerPorProcedimientoAsync(int procedimientoId);

    /// <summary>
    /// Devuelve las reglas activas de una etapa especifica.
    /// Las usa el orquestador para saber que validar en cada etapa.
    /// </summary>
    Task<IReadOnlyList<ReglaValidacion>> ObtenerPorEtapaAsync(int etapaId);

    /// <summary>
    /// Busca una regla por su ID.
    /// </summary>
    Task<ReglaValidacion?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Crea una regla nueva.
    /// Solo el Administrador puede llamar a esto.
    /// </summary>
    Task<ReglaValidacion> CrearAsync(ReglaValidacion regla);

    /// <summary>
    /// Actualiza una regla existente.
    /// Solo el Administrador puede llamar a esto.
    /// </summary>
    Task<ReglaValidacion> ActualizarAsync(ReglaValidacion regla);
}