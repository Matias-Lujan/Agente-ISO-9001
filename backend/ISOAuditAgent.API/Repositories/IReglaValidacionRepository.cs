using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Define el contrato para acceder a las reglas de validación desde la base de datos MySQL.
/// Las reglas son configurables y específicas para cada proceso ISO 9001.
/// </summary>
public interface IReglaValidacionRepository
{
    /// <summary>
    /// Obtiene todas las reglas de validación activas para un proceso específico.
    /// Las reglas definen qué inconsistencias debe detectar el agente de validación.
    /// </summary>
    /// <param name="procesoId">Identificador del proceso en la base de datos.</param>
    /// <returns>Lista de reglas de validación activas para ese proceso.</returns>
    Task<List<ReglaValidacion>> GetReglasByProcesoAsync(int procesoId);

    /// <summary>
    /// Obtiene una regla específica por su ID.
    /// </summary>
    /// <param name="reglaId">Identificador de la regla.</param>
    /// <returns>La regla si existe, null en caso contrario.</returns>
    Task<ReglaValidacion?> GetReglaByIdAsync(int reglaId);

    /// <summary>
    /// Obtiene todas las reglas (activas e inactivas) de un proceso.
    /// Útil para auditoría o análisis histórico.
    /// </summary>
    /// <param name="procesoId">Identificador del proceso.</param>
    /// <param name="incluirInactivas">Si es true, incluye reglas desactivadas.</param>
    /// <returns>Lista de reglas del proceso.</returns>
    Task<List<ReglaValidacion>> GetAllReglasByProcesoAsync(int procesoId, bool incluirInactivas = false);
}
