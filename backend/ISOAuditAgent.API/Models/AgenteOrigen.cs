namespace ISOAuditAgent.API.Models;

/// <summary>
/// Enumeración que identifica cuál agente generó un hallazgo o resultado.
/// Utilizado en la arquitectura MAF (Microsoft Agent Framework) basada en grafos.
/// </summary>
public enum AgenteOrigen
{
    /// <summary>
    /// Hallazgo originado por el Agente de Validación de Cumplimiento (ComplianceValidationAgent).
    /// Detecta inconsistencias entre Trello y Clockify.
    /// </summary>
    ComplianceValidation = 1,

    /// <summary>
    /// Hallazgo originado por el Agente de Análisis de Documentos (DocumentAnalysisAgent).
    /// Analiza documentos y su conformidad con ISO 9001.
    /// </summary>
    DocumentAnalysis = 2,

    /// <summary>
    /// Hallazgo originado por el Agente de Verificación de Consistencia (ConsistencyVerificationAgent).
    /// Verifica la coherencia entre diferentes fuentes de datos.
    /// </summary>
    ConsistencyVerification = 3,

    /// <summary>
    /// Hallazgo originado por el Agente de Clasificación de Hallazgos (FindingsClassificationAgent).
    /// Clasifica hallazgos según severidad, tipo y riesgo.
    /// </summary>
    FindingsClassification = 4
}
