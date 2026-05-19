using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using ISOAuditAgent.DocumentAnalysis.Mcp.Clockify;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.API.Mcp.Clockify;

/// <summary>
/// Cliente HTTP tipado contra el API REST de Clockify. Resuelve la API
/// key desde variable de entorno en cada request y la envía como header
/// <c>X-Api-Key</c>.
/// </summary>
/// <remarks>
/// La validación de credenciales se hace en runtime (no en el ctor) para
/// que la API arranque incluso si <c>CLOCKIFY_API_KEY</c> no está definida
/// — alineado con el patrón de <c>GoogleDriveClient</c> y <c>TrelloApiClient</c>.
/// </remarks>
public sealed class ClockifyApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ClockifyOptions _options;
    private readonly ILogger<ClockifyApiClient> _logger;

    public ClockifyApiClient(
        HttpClient http,
        IOptions<ClockifyOptions> options,
        ILogger<ClockifyApiClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_http.BaseAddress is null)
        {
            _http.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
        }
    }

    /// <summary>
    /// Obtiene el proyecto indicado en el workspace indicado.
    /// </summary>
    public async Task<ClockifyProjectSummary> GetProjectAsync(
        string workspaceId,
        string projectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        var apiKey = RequireApiKey();

        var url = $"workspaces/{Uri.EscapeDataString(workspaceId)}" +
                  $"/projects/{Uri.EscapeDataString(projectId)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await _http
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Clockify: error de red consultando proyecto '{Project}' en workspace '{Workspace}'.",
                projectId, workspaceId);
            throw new InvalidOperationException(
                $"Clockify: error de red consultando proyecto '{projectId}': {ex.Message}", ex);
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                $"Clockify: credenciales rechazadas (HTTP {(int)response.StatusCode}). " +
                "Verifique CLOCKIFY_API_KEY.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                $"Clockify: proyecto '{projectId}' no encontrado en workspace '{workspaceId}' (HTTP 404).");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content
                .ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Clockify respondió HTTP {(int)response.StatusCode} para proyecto '{projectId}': " +
                $"{Truncate(body, 200)}");
        }

        var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var dto = await JsonSerializer
            .DeserializeAsync<ClockifyProjectDto>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (dto is null || string.IsNullOrEmpty(dto.Id))
        {
            throw new InvalidOperationException(
                $"Clockify: respuesta vacía o sin id para proyecto '{projectId}'.");
        }

        return new ClockifyProjectSummary(
            Id: dto.Id,
            WorkspaceId: dto.WorkspaceId ?? workspaceId,
            Name: dto.Name ?? string.Empty,
            Archived: dto.Archived);
    }

    private string RequireApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable(_options.ApiKeyEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Clockify: API key no configurada. Defina la variable de entorno " +
                $"'{_options.ApiKeyEnvironmentVariable}' (Profile → Settings → API).");
        }

        return apiKey;
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";

    private sealed record ClockifyProjectDto(
        string? Id,
        string? WorkspaceId,
        string? Name,
        bool Archived);
}
