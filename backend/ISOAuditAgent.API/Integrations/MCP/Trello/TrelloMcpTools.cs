// ============================================================================
//  TrelloMcpTools — Tools MCP expuestas por el server in-process de Trello
// ----------------------------------------------------------------------------
//  Dos tools mínimas que replican el patrón de DriveMcpTools:
//    get_trello_board(boardId)        → TrelloBoardInfo?
//    list_trello_cards(boardId)       → lista plana de TrelloCardInfo
//
//  Se montan en /mcp/trello vía TrelloMcpExtensions + app.MapMcp.
// ============================================================================

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace ISOAuditAgent.API.Integrations.MCP.Trello;

[McpServerToolType]
public sealed class TrelloMcpTools
{
    private readonly TrelloApiClient _client;
    private readonly ILogger<TrelloMcpTools> _logger;

    public TrelloMcpTools(TrelloApiClient client, ILogger<TrelloMcpTools> logger)
    {
        _client = client;
        _logger = logger;
    }

    [McpServerTool(Name = "get_trello_board")]
    [Description("Obtiene la información de un board de Trello por su ID. " +
                 "Devuelve null si el board no existe. " +
                 "Campos: id, name, closed.")]
    public async Task<TrelloBoardInfo?> GetTrelloBoardAsync(
        [Description("ID del board de Trello.")] string boardId,
        CancellationToken ct = default)
    {
        try
        {
            var board = await _client.GetBoardAsync(boardId, ct);
            _logger.LogInformation("MCP get_trello_board: boardId={BoardId}, found={Found}.",
                boardId, board is not null);
            return board;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP get_trello_board falló para boardId={BoardId}.", boardId);
            throw;
        }
    }

    [McpServerTool(Name = "list_trello_cards")]
    [Description("Lista las tarjetas (cards) de un board de Trello. " +
                 "Devuelve lista vacía si no hay tarjetas. " +
                 "Campos por tarjeta: id, name.")]
    public async Task<IReadOnlyList<TrelloCardInfo>> ListTrelloCardsAsync(
        [Description("ID del board de Trello.")] string boardId,
        CancellationToken ct = default)
    {
        try
        {
            var cards = await _client.ListCardsAsync(boardId, ct);
            _logger.LogInformation("MCP list_trello_cards: boardId={BoardId}, {Count} tarjetas.",
                boardId, cards.Count);
            return cards;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP list_trello_cards falló para boardId={BoardId}.", boardId);
            throw;
        }
    }
}
