// ============================================================================
//  TrelloApiClient — Cliente HTTP para Trello REST API v1 (D6.Trello)
// ----------------------------------------------------------------------------
//  Singleton. Usa HttpClient con timeout de 15 s. Expone dos operaciones
//  mínimas que necesitan las tools MCP:
//   - GetBoardAsync: verifica que el board exista y no esté cerrado.
//   - ListCardsAsync: devuelve los nombres de las tarjetas del board.
//
//  Auth: key + token como query params (Trello API v1 estándar).
//  Cualquier excepción de red/auth/rate-limit se propaga al caller (DriveMcp
//  tools) que la loguea y la re-lanza al MCP server. El checker de nivel
//  superior (TrelloChecker) atrapa y degrada a NoEncontrado.
// ============================================================================

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.API.Integrations.MCP.Trello;

public sealed class TrelloApiClient
{
    private const string BaseUrl = "https://api.trello.com/1";

    private readonly HttpClient _http;
    private readonly TrelloOptions _opts;
    private readonly ILogger<TrelloApiClient> _logger;

    public TrelloApiClient(IOptions<TrelloOptions> opts, ILogger<TrelloApiClient> logger)
    {
        _opts = opts.Value;
        _logger = logger;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    /// <summary>
    /// Devuelve el board si existe. null si no existe (404).
    /// Lanza en caso de auth inválida, rate-limit u otro error HTTP.
    /// </summary>
    public async Task<TrelloBoardInfo?> GetBoardAsync(string boardId, CancellationToken ct)
    {
        var url = $"{BaseUrl}/boards/{boardId}?fields=id,name,closed&key={_opts.ApiKey}&token={_opts.Token}";
        var response = await _http.GetAsync(url, ct).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Trello: board {BoardId} no encontrado (404).", boardId);
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TrelloBoardInfo>(cancellationToken: ct)
               .ConfigureAwait(false);
    }

    /// <summary>
    /// Lista las tarjetas del board (solo campo name).
    /// Lanza en caso de error HTTP.
    /// </summary>
    public async Task<IReadOnlyList<TrelloCardInfo>> ListCardsAsync(string boardId, CancellationToken ct)
    {
        var url = $"{BaseUrl}/boards/{boardId}/cards?fields=id,name&key={_opts.ApiKey}&token={_opts.Token}";
        var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var cards = await response.Content.ReadFromJsonAsync<List<TrelloCardInfo>>(cancellationToken: ct)
                    .ConfigureAwait(false);
        return cards ?? [];
    }
}

public sealed record TrelloBoardInfo(
    [property: JsonPropertyName("id")]   string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("closed")] bool Closed);

public sealed record TrelloCardInfo(
    [property: JsonPropertyName("id")]   string Id,
    [property: JsonPropertyName("name")] string Name);
