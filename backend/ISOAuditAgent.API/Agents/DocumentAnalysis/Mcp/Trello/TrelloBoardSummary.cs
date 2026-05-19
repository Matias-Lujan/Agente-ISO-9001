namespace ISOAuditAgent.DocumentAnalysis.Mcp.Trello;

/// <summary>
/// Resumen mínimo de un board de Trello para auditoría (Fase F.5).
/// El sistema valida existencia del tablero, no contenido — sin tarjetas
/// ni miembros. <see cref="IdLists"/> entra solo para alimentar el hash
/// canónico y detectar reorganizaciones estructurales entre auditorías.
/// </summary>
/// <remarks>
/// Producido por la tool MCP <c>get_project_board</c> /
/// <c>get_board_by_url</c> en el host, y consumido por el cliente MCP del
/// agente. Vive en el ensamblado de DocumentAnalysis para que ambos lados
/// compartan el contrato sin que el host quede como única fuente.
/// </remarks>
public sealed record TrelloBoardSummary(
    string Id,
    string Name,
    string? Desc,
    string Url,
    bool Closed,
    DateTimeOffset? DateLastActivity,
    IReadOnlyList<string> IdLists);
