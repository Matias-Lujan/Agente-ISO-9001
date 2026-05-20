using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ISOAuditAgent.DocumentAnalysis.Mcp.Trello;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.API.Mcp.Trello;

/// <summary>
/// Cliente HTTP tipado contra el API REST de Trello. Resuelve credenciales
/// (API key + token) desde variables de entorno en cada request.
/// </summary>
/// <remarks>
/// <para>
/// La validación de credenciales se hace en runtime (no en el ctor) para
/// que la API arranque incluso si <c>TRELLO_API_KEY</c> / <c>TRELLO_TOKEN</c>
/// no están definidas — alineado con el patrón de <c>GoogleDriveClient</c>.
/// El error de credenciales faltantes se relanza en <c>TrelloMcpTools</c>
/// como <c>McpException</c> con mensaje útil.
/// </para>
/// <para>
/// El <see cref="HttpClient"/> se inyecta vía <c>IHttpClientFactory</c>
/// (configurado en <c>TrelloMcpExtensions</c>).
/// </para>
/// </remarks>
public sealed class TrelloApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly TrelloOptions _options;
    private readonly ILogger<TrelloApiClient> _logger;

    public TrelloApiClient(
        HttpClient http,
        IOptions<TrelloOptions> options,
        ILogger<TrelloApiClient> logger)
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
    /// Obtiene el board indicado por id o shortLink. Resuelve también las
    /// listas (idLists) con una segunda llamada para alimentar el hash.
    /// </summary>
    public async Task<TrelloBoardSummary> GetBoardAsync(
        string boardIdOrShortLink,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boardIdOrShortLink);
        var (apiKey, token) = RequireCredentials();

        var boardFields = "name,desc,url,closed,dateLastActivity";
        var boardUrl = $"boards/{Uri.EscapeDataString(boardIdOrShortLink)}" +
                       $"?fields={boardFields}&key={apiKey}&token={token}";

        var board = await GetAndDeserializeAsync<TrelloBoardDto>(
            boardUrl, boardIdOrShortLink, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(board.Id))
        {
            throw new InvalidOperationException(
                $"Trello devolvió un board sin id para '{boardIdOrShortLink}'.");
        }

        var listsUrl = $"boards/{Uri.EscapeDataString(board.Id)}/lists" +
                       $"?fields=id&filter=open&key={apiKey}&token={token}";

        var lists = await GetAndDeserializeAsync<TrelloListDto[]>(
            listsUrl, boardIdOrShortLink, cancellationToken).ConfigureAwait(false);

        var idLists = lists
            .Select(l => l.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        return new TrelloBoardSummary(
            Id: board.Id,
            Name: board.Name ?? string.Empty,
            Desc: string.IsNullOrEmpty(board.Desc) ? null : board.Desc,
            Url: board.Url ?? string.Empty,
            Closed: board.Closed,
            DateLastActivity: board.DateLastActivity,
            IdLists: idLists);
    }

    private async Task<T> GetAndDeserializeAsync<T>(
        string relativeUrl,
        string boardRef,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http
                .GetAsync(relativeUrl, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Trello: error de red consultando board '{Board}'.", boardRef);
            throw new InvalidOperationException(
                $"Trello: error de red consultando '{boardRef}': {ex.Message}", ex);
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new InvalidOperationException(
                $"Trello: credenciales rechazadas (HTTP {(int)response.StatusCode}). " +
                "Verifique TRELLO_API_KEY y TRELLO_TOKEN.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException(
                $"Trello: board '{boardRef}' no encontrado (HTTP 404).");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content
                .ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Trello respondió HTTP {(int)response.StatusCode} para '{boardRef}': " +
                $"{Truncate(body, 200)}");
        }

        var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var value = await JsonSerializer
            .DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (value is null)
        {
            throw new InvalidOperationException(
                $"Trello: respuesta vacía o no deserializable para '{boardRef}'.");
        }

        return value;
    }

    private (string ApiKey, string Token) RequireCredentials()
    {
        var apiKey = Environment.GetEnvironmentVariable(_options.ApiKeyEnvironmentVariable);
        var token = Environment.GetEnvironmentVariable(_options.TokenEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                $"Trello: credenciales no configuradas. Defina las variables de entorno " +
                $"'{_options.ApiKeyEnvironmentVariable}' y '{_options.TokenEnvironmentVariable}' " +
                "(API key: https://trello.com/app-key; token: link 'Token' debajo de la API key).");
        }

        return (apiKey, token);
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";

    private sealed record TrelloBoardDto(
        string? Id,
        string? Name,
        string? Desc,
        string? Url,
        bool Closed,
        DateTimeOffset? DateLastActivity);

    private sealed record TrelloListDto(string? Id);
}
