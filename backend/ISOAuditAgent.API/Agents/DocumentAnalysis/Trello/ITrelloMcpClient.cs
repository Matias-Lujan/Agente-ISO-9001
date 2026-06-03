using ISOAuditAgent.API.Integrations.MCP.Trello;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Trello;

/// <summary>
/// Abstracción del cliente MCP de Trello. Espejo 1:1 de las tools del server.
/// </summary>
public interface ITrelloMcpClient
{
    Task<TrelloBoardInfo?> GetBoardAsync(string boardId, CancellationToken ct = default);
    Task<IReadOnlyList<TrelloCardInfo>> ListCardsAsync(string boardId, CancellationToken ct = default);
}
