using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.ComplianceValidation;

/// <summary>
/// Define el contrato para el Agente de Validación de Cumplimiento (Compliance Validation Agent).
/// Este agente detecta inconsistencias entre Trello y Clockify según reglas configurables.
/// NO clasifica los hallazgos; solo detecta desvíos. La clasificación es responsabilidad del Agente de Clasificación.
/// </summary>
public interface IComplianceValidationAgent
{
    /// <summary>
    /// Valida un proceso específico cruzando datos de Trello y Clockify.
    /// </summary>
    /// <param name="projectId">Identificador del proyecto (usado para consultar Trello y Clockify vía MCP).</param>
    /// <param name="procesoId">Identificador del proceso ISO 9001 (usado para obtener reglas de la BD).</param>
    /// <returns>Lista de hallazgos de inconsistencias detectadas. Si no hay inconsistencias, retorna lista vacía.</returns>
    Task<List<ValidationFinding>> ValidateProcessAsync(string projectId, int procesoId);
}
