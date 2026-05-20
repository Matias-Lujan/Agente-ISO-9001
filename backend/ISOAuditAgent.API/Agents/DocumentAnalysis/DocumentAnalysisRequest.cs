namespace ISOAuditAgent.DocumentAnalysis;

/// <summary>
/// Request del agente DocumentAnalysis, desacoplado del input del workflow MAF.
/// El host/orquestador es responsable del mapeo
/// <c>IniciarAuditoriaWorkflowInput → DocumentAnalysisRequest</c>.
/// </summary>
/// <remarks>
/// <para>
/// Diseño alineado con <c>contratos_agentes.md §3.1</c>. El campo
/// <c>EtapaId</c> es la etapa actual del proyecto indicada por el usuario
/// y determina la exigibilidad de cada artefacto.
/// </para>
/// <para>
/// <c>SolicitudEnUtc</c> se mantiene para trazabilidad operativa del
/// runner (logs, telemetría); no forma parte del contrato del workflow.
/// </para>
/// </remarks>
public sealed record DocumentAnalysisRequest(
    int AuditoriaId,
    int ProyectoId,
    int EtapaId,
    DateTimeOffset SolicitudEnUtc
);
