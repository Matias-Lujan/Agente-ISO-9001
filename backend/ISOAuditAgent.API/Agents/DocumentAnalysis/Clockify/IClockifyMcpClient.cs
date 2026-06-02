using ISOAuditAgent.API.Integrations.MCP.Clockify;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Clockify;

public interface IClockifyMcpClient
{
    Task<ClockifyProjectInfo?> GetProjectAsync(
        string workspaceId, string projectId, CancellationToken ct = default);

    /// <summary>Horas totales registradas en el proyecto, en segundos. 0 = sin horas.</summary>
    Task<long> GetProjectTotalSecondsAsync(
        string workspaceId, string projectId, CancellationToken ct = default);
}
