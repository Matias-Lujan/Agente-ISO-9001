using System.Text.RegularExpressions;

namespace ISOAuditAgent.API.Mcp.Clockify;

/// <summary>
/// Extrae <c>projectId</c> (y opcionalmente <c>workspaceId</c>) de URLs
/// típicas de Clockify.
/// </summary>
/// <remarks>
/// Formatos soportados:
/// <list type="bullet">
///   <item><description><c>https://app.clockify.me/projects/{projectId}</c></description></item>
///   <item><description><c>https://app.clockify.me/workspaces/{workspaceId}/projects/{projectId}</c></description></item>
///   <item><description><c>https://app.clockify.me/tracker?project={projectId}</c></description></item>
/// </list>
/// Si la URL no trae workspace, el caller usa el <c>clockify_workspace_id</c>
/// global de <c>ConfiguracionSistema</c> (o la env var equivalente).
/// </remarks>
public static partial class ClockifyProjectUrl
{
    public static ClockifyUrlInfo? TryExtract(string clockifyUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clockifyUrl);

        // 1) /workspaces/{ws}/projects/{pid}  (más específico primero)
        var wsThenProject = WorkspaceThenProject().Match(clockifyUrl);
        if (wsThenProject.Success)
        {
            return new ClockifyUrlInfo(
                ProjectId: wsThenProject.Groups[2].Value,
                WorkspaceId: wsThenProject.Groups[1].Value);
        }

        // 2) /projects/{pid}
        var projectsPath = ProjectsPath().Match(clockifyUrl);
        if (projectsPath.Success)
        {
            return new ClockifyUrlInfo(
                ProjectId: projectsPath.Groups[1].Value,
                WorkspaceId: null);
        }

        // 3) /tracker?project={pid}
        var trackerQuery = TrackerProjectQuery().Match(clockifyUrl);
        if (trackerQuery.Success)
        {
            return new ClockifyUrlInfo(
                ProjectId: trackerQuery.Groups[1].Value,
                WorkspaceId: null);
        }

        return null;
    }

    [GeneratedRegex(@"/workspaces/([a-zA-Z0-9]{8,})/projects/([a-zA-Z0-9]{8,})",
        RegexOptions.CultureInvariant)]
    private static partial Regex WorkspaceThenProject();

    [GeneratedRegex(@"/projects/([a-zA-Z0-9]{8,})",
        RegexOptions.CultureInvariant)]
    private static partial Regex ProjectsPath();

    [GeneratedRegex(@"[?&]project=([a-zA-Z0-9]{8,})",
        RegexOptions.CultureInvariant)]
    private static partial Regex TrackerProjectQuery();
}

/// <summary>Resultado de parsear una URL de Clockify.</summary>
public sealed record ClockifyUrlInfo(string ProjectId, string? WorkspaceId);
