using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.ComplianceValidation;

/// <summary>
/// Define el contrato para el Agente de Validación de Cumplimiento (Compliance Validation Agent).
/// Implementa el patrón de arquitectura MAF (Microsoft Agent Framework) basada en grafos.
/// 
/// Este agente detecta inconsistencias entre Trello y Clockify según reglas configurables.
/// NO clasifica los hallazgos; solo detecta desvíos. La clasificación es responsabilidad del Agente de Clasificación.
/// </summary>
public interface IComplianceValidationAgent
{
    /// <summary>
    /// Ejecuta el análisis de validación de cumplimiento de manera asincrónica.
    /// Punto de entrada estándar para la arquitectura MAF.
    /// 
    /// Recibe datos extraídos (AuditoriaId, ProyectoId, Documentos) y retorna hallazgos preliminares
    /// con evidencias y justificaciones para cada inconsistencia detectada.
    /// </summary>
    /// <param name="input">Datos de entrada conteniendo AuditoriaId, ProyectoId, y documentos a analizar.</param>
    /// <returns>Hallazgos preliminares detectados, incluyendo evidencias y justificaciones.</returns>
    Task<HallazgosPreliminares> ExecuteAsync(DocumentosExtraidos input);

    /// <summary>
    /// Valida un proceso específico cruzando datos de Trello y Clockify.
    /// Método heredado para compatibilidad con código existente.
    /// </summary>
    /// <param name="projectId">Identificador del proyecto (usado para consultar Trello y Clockify vía MCP).</param>
    /// <param name="procesoId">Identificador del proceso ISO 9001 (usado para obtener reglas de la BD).</param>
    /// <returns>Lista de hallazgos de inconsistencias detectadas. Si no hay inconsistencias, retorna lista vacía.</returns>
    [Obsolete("Use ExecuteAsync(DocumentosExtraidos) en su lugar.", false)]
    Task<List<ValidationFinding>> ValidateProcessAsync(string projectId, int procesoId);
}
