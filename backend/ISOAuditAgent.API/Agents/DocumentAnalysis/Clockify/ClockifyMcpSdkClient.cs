// Patrón idéntico a DriveMcpSdkClient y TrelloMcpSdkClient.
using System.Text.Json;
using ISOAuditAgent.API.Integrations.MCP.Clockify;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Clockify;

public sealed class ClockifyMcpSdkClient : IClockifyMcpClient, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly McpClockifyClientOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ClockifyMcpSdkClient> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private McpClient? _mcp;

    public ClockifyMcpSdkClient(
        IOptions<McpClockifyClientOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<ClockifyMcpSdkClient> logger)
    {
        _options = options.Value;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task<ClockifyProjectInfo?> GetProjectAsync(
        string workspaceId, string projectId, CancellationToken ct)
    {
        var client = await GetOrCreateClientAsync(ct).ConfigureAwait(false);
        var result = await client.CallToolAsync(
            "get_clockify_project",
            new Dictionary<string, object?> { ["workspaceId"] = workspaceId, ["projectId"] = projectId },
            cancellationToken: ct).ConfigureAwait(false);

        if (result.IsError == true) return null;
        var json = ExtractJson(result);
        if (json == "{}" || string.IsNullOrWhiteSpace(json) || json == "null") return null;
        return JsonSerializer.Deserialize<ClockifyProjectInfo>(json, JsonOptions);
    }

    public async Task<long> GetProjectTotalSecondsAsync(
        string workspaceId, string projectId, CancellationToken ct)
    {
        var client = await GetOrCreateClientAsync(ct).ConfigureAwait(false);
        var result = await client.CallToolAsync(
            "get_clockify_project_duration",
            new Dictionary<string, object?> { ["workspaceId"] = workspaceId, ["projectId"] = projectId },
            cancellationToken: ct).ConfigureAwait(false);

        if (result.IsError == true) return 0L;
        var json = ExtractJson(result);
        if (string.IsNullOrWhiteSpace(json) || json == "{}") return 0L;
        return long.TryParse(json.Trim('"'), out var seconds) ? seconds : 0L;
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
                    Name = "ClockifyMcpClient"
                }, _loggerFactory);
            _mcp = await McpClient.CreateAsync(transport, new McpClientOptions(), _loggerFactory, ct)
                .ConfigureAwait(false);
            _logger.LogInformation("ClockifyMcpSdkClient: conectado a {Endpoint}.", _options.Endpoint);
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
