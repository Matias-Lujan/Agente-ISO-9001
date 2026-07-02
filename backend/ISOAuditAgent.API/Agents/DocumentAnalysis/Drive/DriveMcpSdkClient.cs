// ============================================================================
//  DriveMcpSdkClient — Implementación de IDriveMcpClient con SDK MCP (D3.2)
// ----------------------------------------------------------------------------
//  Conexión perezosa al server MCP /mcp/drive vía HttpClientTransport del
//  SDK oficial. La conexión se crea en el primer call y se reusa entre calls
//  (el handshake MCP no es barato — repetirlo por cada call sería caro).
//
//  THREAD-SAFETY DE LA CONEXIÓN PEREZOSA:
//   SemaphoreSlim(1,1) garantiza que dos requests concurrentes que arriben
//   antes de que _mcp esté inicializado no creen dos conexiones distintas.
//   Patrón validado en runtime por el compañero (DriveMcpSdkClient del diff
//   viejo). Doble check del null (fuera + dentro del semáforo) evita tomar
//   el lock cuando _mcp ya existe — fast path.
//
//  EXTRACCIÓN DEL JSON DE LA RESPUESTA:
//   SDK MCP 1.2.0 puede serializar el resultado en result.StructuredContent
//   o en result.Content como TextContentBlock. Hay que probar los dos —
//   esto se descubrió en D3.1: leer solo StructuredContent devolvía "{}".
//   Patrón rescatado del compañero (DriveMcpSdkClient.GetStructuredJsonString).
//
//  REGISTRO DI:
//   Singleton — la conexión MCP es estado caro a recrear. Una instancia por
//   todo el proceso. SemaphoreSlim + IDisposable manejan el ciclo de vida.
// ============================================================================

using System.Text.Json;
using ISOAuditAgent.API.Integrations.MCP.Drive;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Drive;

public sealed class DriveMcpSdkClient : IDriveMcpClient, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly McpDriveClientOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DriveMcpSdkClient> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private McpClient? _mcp;

    public DriveMcpSdkClient(
        IOptions<McpDriveClientOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<DriveMcpSdkClient> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentException.ThrowIfNullOrWhiteSpace(_options.Endpoint);
    }

    public async Task<DriveFolderListing> ListFilesUnderFolderAsync(
        string folderId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId);

        var client = await GetOrCreateClientAsync(ct).ConfigureAwait(false);

        var result = await client.CallToolAsync(
            "list_files_under_folder",
            new Dictionary<string, object?> { ["folderId"] = folderId },
            cancellationToken: ct).ConfigureAwait(false);

        ThrowIfToolError("list_files_under_folder", result);

        var json = ExtractMcpJson(result);
        var listing = JsonSerializer.Deserialize<DriveFolderListing>(json, JsonOptions)
            ?? throw new InvalidOperationException(
                "list_files_under_folder: respuesta MCP vacía o no deserializable.");

        return listing;
    }

    public async Task<DriveFileContent> GetFileContentAsync(
        string fileId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        var client = await GetOrCreateClientAsync(ct).ConfigureAwait(false);

        var result = await client.CallToolAsync(
            "get_file_content",
            new Dictionary<string, object?> { ["fileId"] = fileId },
            cancellationToken: ct).ConfigureAwait(false);

        ThrowIfToolError("get_file_content", result);

        var json = ExtractMcpJson(result);
        var content = JsonSerializer.Deserialize<DriveFileContent>(json, JsonOptions)
            ?? throw new InvalidOperationException(
                "get_file_content: respuesta MCP vacía o no deserializable.");

        return content;
    }

    private async Task<McpClient> GetOrCreateClientAsync(CancellationToken ct)
    {
        // Fast path: si ya está creado, evitar el lock.
        if (_mcp is not null) return _mcp;

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Double-check dentro del lock: otro hilo pudo haberlo creado.
            if (_mcp is not null) return _mcp;

            var endpoint = new Uri(_options.Endpoint, UriKind.Absolute);

            var transport = new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = endpoint,
                    Name = "DriveMcpClient"
                },
                _loggerFactory);

            var mcp = await McpClient.CreateAsync(
                    transport,
                    new McpClientOptions(),
                    _loggerFactory,
                    ct)
                .ConfigureAwait(false);

            _mcp = mcp;

            _logger.LogInformation(
                "DriveMcpSdkClient: conectado a {Endpoint}.", endpoint);

            return _mcp;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void ThrowIfToolError(string toolName, CallToolResult result)
    {
        if (result.IsError != true) return;

        var detail = ExtractMcpJson(result);
        throw new InvalidOperationException(
            $"MCP tool '{toolName}' devolvió error: {detail}");
    }

    /// <summary>
    /// Extrae el JSON del resultado MCP. SDK 1.2.0 puede ponerlo en
    /// StructuredContent (preferido) o en Content como TextContentBlock
    /// (fallback). Sin el fallback devuelve "{}" silenciosamente.
    /// Patrón validado por el compañero en runtime.
    /// </summary>
    private static string ExtractMcpJson(CallToolResult result)
    {
        if (result.StructuredContent is JsonElement element)
            return element.GetRawText();

        if (result.Content is { Count: > 0 })
        {
            foreach (var block in result.Content)
            {
                if (block is TextContentBlock text && !string.IsNullOrWhiteSpace(text.Text))
                    return text.Text;
            }
        }

        return "{}";
    }

    public async ValueTask DisposeAsync()
    {
        if (_mcp is not null)
        {
            await _mcp.DisposeAsync().ConfigureAwait(false);
            _mcp = null;
        }
        _gate.Dispose();
    }
}