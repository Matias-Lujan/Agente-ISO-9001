// ============================================================================
//  ClockifyApiClient — Cliente HTTP para Clockify REST API v1 + Reports API
// ----------------------------------------------------------------------------
//  Singleton. Dos HttpClients con timeout de 15 s:
//   - _http        → api.clockify.me/api/v1  (proyectos, tiempo, etc.)
//   - _httpReports → reports.api.clockify.me/v1 (duración agregada)
//  Ambos usan el mismo X-Api-Key.
//
//  GetProjectAsync:
//   GET /workspaces/{wid}/projects/{pid} → ClockifyProjectInfo? (null = 404)
//
//  GetProjectTotalSecondsAsync:
//   POST reports.api.clockify.me/v1/workspaces/{wid}/reports/summary
//   Body: { summaryFilter.groups=["PROJECT"], dateRangeType="ALL_TIME",
//           projects.ids=[pid] }
//   Response: totals[0].totalTime (segundos, long). 0 si sin horas.
// ============================================================================

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.API.Integrations.MCP.Clockify;

public sealed class ClockifyApiClient
{
    private const string MainBaseUrl    = "https://api.clockify.me/api/v1";
    private const string ReportsBaseUrl = "https://reports.api.clockify.me/v1";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly HttpClient _httpReports;
    private readonly ClockifyOptions _opts;
    private readonly ILogger<ClockifyApiClient> _logger;

    public ClockifyApiClient(IOptions<ClockifyOptions> opts, ILogger<ClockifyApiClient> logger)
    {
        _opts = opts.Value;
        _logger = logger;

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _http.DefaultRequestHeaders.Add("X-Api-Key", _opts.ApiKey);

        _httpReports = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _httpReports.DefaultRequestHeaders.Add("X-Api-Key", _opts.ApiKey);
    }

    /// <summary>
    /// Devuelve el proyecto si existe. null si no existe (404).
    /// Lanza en caso de auth inválida u otro error HTTP.
    /// </summary>
    public async Task<ClockifyProjectInfo?> GetProjectAsync(
        string workspaceId, string projectId, CancellationToken ct)
    {
        var url = $"{MainBaseUrl}/workspaces/{workspaceId}/projects/{projectId}";
        var response = await _http.GetAsync(url, ct).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation(
                "Clockify: proyecto {ProjectId} en workspace {WorkspaceId} no encontrado (404).",
                projectId, workspaceId);
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClockifyProjectInfo>(cancellationToken: ct)
               .ConfigureAwait(false);
    }

    /// <summary>
    /// Devuelve las horas totales registradas (en segundos) para el proyecto,
    /// consultando la Reports API con rango ALL_TIME.
    /// Devuelve 0 si el proyecto existe pero no tiene horas.
    /// Lanza en caso de error HTTP.
    /// </summary>
    public async Task<long> GetProjectTotalSecondsAsync(
        string workspaceId, string projectId, CancellationToken ct)
    {
        var url = $"{ReportsBaseUrl}/workspaces/{workspaceId}/reports/summary";

        var body = new
        {
            summaryFilter = new { groups = new[] { "PROJECT" } },
            dateRangeType = "ALL_TIME",
            projects = new { ids = new[] { projectId } }
        };

        var json = JsonSerializer.Serialize(body, JsonOpts);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpReports.PostAsync(url, content, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var summary = await response.Content
            .ReadFromJsonAsync<ClockifyReportsSummary>(cancellationToken: ct)
            .ConfigureAwait(false);

        return summary?.Totals?.FirstOrDefault()?.TotalTime ?? 0L;
    }
}

public sealed record ClockifyProjectInfo(
    [property: JsonPropertyName("id")]       string Id,
    [property: JsonPropertyName("name")]     string Name,
    [property: JsonPropertyName("archived")] bool Archived);

public sealed record ClockifyReportsSummary(
    [property: JsonPropertyName("totals")] List<ClockifyReportTotal>? Totals);

public sealed record ClockifyReportTotal(
    [property: JsonPropertyName("totalTime")] long TotalTime);
