namespace ISOAuditAgent.DocumentAnalysis;

/// <summary>
/// Request del runner de DocumentAnalysis, desacoplado del input del workflow MAF.
/// El host/orquestador es responsable del mapeo
/// <c>IniciarAuditoriaWorkflowInput → DocumentAnalysisRequest</c>.
/// </summary>
/// <remarks>
/// Diseño alineado con <c>docs/agente-analisis-documental.md</c> §5.2.
/// Extensible: en futuras fases pueden añadirse correlación, modo dry-run, etc.
/// </remarks>
public sealed record DocumentAnalysisRequest(
    int AuditoriaId,
    int ProyectoId,
    DateTimeOffset SolicitudEnUtc
);
