// ============================================================================
//  TrelloMcpSdkClient — Implementación de ITrelloMcpClient (D6.Trello)
// ----------------------------------------------------------------------------
//  Patrón idéntico a DriveMcpSdkClient: conexión perezosa al server MCP
//  /mcp/trello vía HttpClientTransport, SemaphoreSlim thread-safe.
// ============================================================================

using System.Text.Json;
using ISOAuditAgent.API.Integrations.MCP.Trello;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Trello;

public sealed class TrelloMcpSdkClient : ITrelloMcpClient, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly McpTrelloClientOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TrelloMcpSdkClient> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private McpClient? _mcp;

    public TrelloMcpSdkClient(
        IOptions<McpTrelloClientOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<TrelloMcpSdkClient> logger)
    {
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task<TrelloBoardInfo?> GetBoardAsync(string boardId, CancellationToken ct)
    {
        var client = await GetOrCreateClientAsync(ct).ConfigureAwait(false);
        var result = await client.CallToolAsync(
            "get_trello_board",
            new Dictionary<string, object?> { ["boardId"] = boardId },
            cancellationToken: ct).ConfigureAwait(false);

        if (result.IsError == true) return null;
        var json = ExtractJson(result);
        if (json == "{}" || string.IsNullOrWhiteSpace(json) || json == "null") return null;
        return JsonSerializer.Deserialize<TrelloBoardInfo>(json, JsonOptions);
    }

    public async Task<IReadOnlyList<TrelloCardInfo>> ListCardsAsync(string boardId, CancellationToken ct)
    {
        var client = await GetOrCreateClientAsync(ct).ConfigureAwait(false);
        var result = await client.CallToolAsync(
            "list_trello_cards",
            new Dictionary<string, object?> { ["boardId"] = boardId },
            cancellationToken: ct).ConfigureAwait(false);

        if (result.IsError == true) return [];
        var json = ExtractJson(result);
        return JsonSerializer.Deserialize<List<TrelloCardInfo>>(json, JsonOptions) ?? [];
    }

    private async Task<McpClient> GetOrCreateClientAsync(CancellationToken ct)
    {
        if (_mcp is not null) return _mcp;
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_mcp is not null) return _mcp;
            var transport = new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = new Uri(_options.Endpoint, UriKind.Absolute),
                    Name = "TrelloMcpClient"
                }, _loggerFactory);
            _mcp = await McpClient.CreateAsync(transport, new McpClientOptions(), _loggerFactory, ct)
                .ConfigureAwait(false);
            _logger.LogInformation("TrelloMcpSdkClient: conectado a {Endpoint}.", _options.Endpoint);
            return _mcp;
        }
        finally { _gate.Release(); }
    }

    private static string ExtractJson(CallToolResult result)
    {
        if (result.StructuredContent is System.Text.Json.JsonElement el) return el.GetRawText();
        if (result.Content is { Count: > 0 })
            foreach (var block in result.Content)
                if (block is TextContentBlock text && !string.IsNullOrWhiteSpace(text.Text))
                    return text.Text;
        return "{}";
    }

    public async ValueTask DisposeAsync()
    {
        if (_mcp is not null) { await _mcp.DisposeAsync(); _mcp = null; }
        _gate.Dispose();
    }
}
