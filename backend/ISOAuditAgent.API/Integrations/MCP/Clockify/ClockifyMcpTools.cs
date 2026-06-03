using System.ComponentModel;
using ModelContextProtocol.Server;

namespace ISOAuditAgent.API.Integrations.MCP.Clockify;

[McpServerToolType]
public sealed class ClockifyMcpTools
{
    private readonly ClockifyApiClient _client;
    private readonly ILogger<ClockifyMcpTools> _logger;

    public ClockifyMcpTools(ClockifyApiClient client, ILogger<ClockifyMcpTools> logger)
    {
        _client = client;
        _logger = logger;
    }

    [McpServerTool(Name = "get_clockify_project")]
    [Description("Obtiene la información de un proyecto de Clockify. " +
                 "Devuelve null si el proyecto no existe. " +
                 "Campos: id, name, archived.")]
    public async Task<ClockifyProjectInfo?> GetClockifyProjectAsync(
        [Description("ID del workspace de Clockify.")] string workspaceId,
        [Description("ID del proyecto de Clockify.")] string projectId,
        CancellationToken ct = default)
    {
        try
        {
            var project = await _client.GetProjectAsync(workspaceId, projectId, ct);
            _logger.LogInformation(
                "MCP get_clockify_project: workspace={WsId}, project={ProjId}, found={Found}.",
                workspaceId, projectId, project is not null);
            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "MCP get_clockify_project falló para workspace={WsId}, project={ProjId}.",
                workspaceId, projectId);
            throw;
        }
    }

    [McpServerTool(Name = "get_clockify_project_duration")]
    [Description("Devuelve las horas totales registradas en un proyecto de Clockify " +
                 "(ALL_TIME), consultando la Reports API. " +
                 "Resultado en segundos (long). 0 = sin horas registradas.")]
    public async Task<long> GetClockifyProjectDurationAsync(
        [Description("ID del workspace de Clockify.")] string workspaceId,
        [Description("ID del proyecto de Clockify.")] string projectId,
        CancellationToken ct = default)
    {
        try
        {
            var seconds = await _client.GetProjectTotalSecondsAsync(workspaceId, projectId, ct);
            _logger.LogInformation(
                "MCP get_clockify_project_duration: workspace={WsId}, project={ProjId}, totalTime={Sec}s.",
                workspaceId, projectId, seconds);
            return seconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "MCP get_clockify_project_duration falló para workspace={WsId}, project={ProjId}.",
                workspaceId, projectId);
            throw;
        }
    }
}
