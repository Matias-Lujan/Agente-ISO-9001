namespace ISOAuditAgent.DocumentAnalysis.Mcp.Clockify;

/// <summary>
/// Resumen mínimo de un proyecto de Clockify para auditoría (Fase F.5).
/// El sistema valida existencia del proyecto, no contenido — sin entries
/// de tiempo ni tasks.
/// </summary>
/// <remarks>
/// Producido por la tool MCP <c>get_project_summary</c> /
/// <c>get_project_by_url</c> en el host, y consumido por el cliente MCP
/// del agente. Vive en el ensamblado de DocumentAnalysis para que ambos
/// lados compartan el contrato.
/// </remarks>
public sealed record ClockifyProjectSummary(
    string Id,
    string WorkspaceId,
    string Name,
    bool Archived);
