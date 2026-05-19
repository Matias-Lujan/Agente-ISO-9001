using System.Text.Json;
using System.Text.Json.Serialization;
using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ISOAuditAgent.DocumentAnalysis.Mcp.Drive;

/// <summary>
/// Implementación de <see cref="IDriveMcpClient"/> usando el SDK oficial MCP
/// sobre HTTP Streamable hacia el endpoint configurado.
/// </summary>
/// <remarks>
/// La conexión al servidor es <b>perezosa</b> (primer llamado) para evitar
/// fallos si el <c>WebApplication</c> aún no escucha durante la construcción
/// del contenedor DI.
/// </remarks>
public sealed class DriveMcpSdkClient : IDriveMcpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
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
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentException.ThrowIfNullOrWhiteSpace(_options.Endpoint);
    }

    /// <inheritdoc />
    public async Task<DriveFileListing> ListProjectFilesAsync(
        int proyectoId,
        CancellationToken cancellationToken = default)
    {
        var client = await GetOrCreateClientAsync(cancellationToken).ConfigureAwait(false);

        var result = await client.CallToolAsync(
            "list_project_files",
            new Dictionary<string, object?> { ["proyectoId"] = proyectoId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        ThrowIfToolError("list_project_files", result);

        var json = GetStructuredJsonString(result);
        var listing = JsonSerializer.Deserialize<DriveFileListing>(json, JsonOptions);
        if (listing is null)
            throw new InvalidOperationException("list_project_files: respuesta MCP vacía o no reconocida.");

        return listing;
    }

    /// <inheritdoc />
    public async Task<DriveFileContent> GetFileContentAsync(
        string fileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        var client = await GetOrCreateClientAsync(cancellationToken).ConfigureAwait(false);

        var result = await client.CallToolAsync(
            "get_file_content",
            new Dictionary<string, object?> { ["fileId"] = fileId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        ThrowIfToolError("get_file_content", result);

        var json = GetStructuredJsonString(result);
        var content = JsonSerializer.Deserialize<DriveFileContent>(json, JsonOptions);
        if (content is null)
            throw new InvalidOperationException("get_file_content: respuesta MCP vacía o no reconocida.");

        return content;
    }

    /// <inheritdoc />
    public Task<DriveFileContent> GetFileContentByDriveUrlAsync(
        string driveUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveUrl);
        return GetFileContentByUrlCoreAsync(driveUrl.Trim(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DriveFolderListing> ListFilesUnderFolderAsync(
        string folderId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId);

        var client = await GetOrCreateClientAsync(cancellationToken).ConfigureAwait(false);

        var result = await client.CallToolAsync(
            "list_files_under_folder",
            new Dictionary<string, object?> { ["folderId"] = folderId },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        ThrowIfToolError("list_files_under_folder", result);

        var json = GetStructuredJsonString(result);
        var listing = JsonSerializer.Deserialize<DriveFolderListing>(json, JsonOptions);
        if (listing is null)
            throw new InvalidOperationException("list_files_under_folder: respuesta MCP vacía o no reconocida.");

        return listing;
    }

    private async Task<DriveFileContent> GetFileContentByUrlCoreAsync(
        string driveUrl,
        CancellationToken cancellationToken)
    {
        var client = await GetOrCreateClientAsync(cancellationToken).ConfigureAwait(false);

        var result = await client.CallToolAsync(
            "get_file_content_by_url",
            new Dictionary<string, object?> { ["driveUrl"] = driveUrl },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        ThrowIfToolError("get_file_content_by_url", result);

        var json = GetStructuredJsonString(result);
        var content = JsonSerializer.Deserialize<DriveFileContent>(json, JsonOptions);
        if (content is null)
            throw new InvalidOperationException("get_file_content_by_url: respuesta MCP vacía o no reconocida.");

        return content;
    }

    private async Task<McpClient> GetOrCreateClientAsync(CancellationToken cancellationToken)
    {
        if (_mcp is not null)
            return _mcp;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_mcp is not null)
                return _mcp;

            var uri = new Uri(_options.Endpoint, UriKind.Absolute);
            var transportOptions = new HttpClientTransportOptions
            {
                Endpoint = uri,
                TransportMode = _options.TransportMode,
                Name = "DriveMcpLocal"
            };

            var transport = new HttpClientTransport(transportOptions, _loggerFactory);
            var mcp = await McpClient.CreateAsync(
                    transport,
                    new McpClientOptions(),
                    _loggerFactory,
                    cancellationToken)
                .ConfigureAwait(false);

            _mcp = mcp;
            _logger.LogInformation(
                "DriveMcpSdkClient: conectado a MCP Drive en {Endpoint}.",
                uri);
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

        var detail = GetStructuredJsonString(result);
        throw new InvalidOperationException(
            $"MCP tool '{toolName}' devolvió error: {detail}");
    }

    private static string GetStructuredJsonString(CallToolResult result)
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
}
