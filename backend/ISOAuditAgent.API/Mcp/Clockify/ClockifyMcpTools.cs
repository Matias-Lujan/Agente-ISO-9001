using System.ComponentModel;
using ISOAuditAgent.DocumentAnalysis.Mcp.Clockify;
using ISOAuditAgent.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ISOAuditAgent.API.Mcp.Clockify;

/// <summary>
/// Herramientas MCP de Clockify (Fase F.5.2). Validan existencia del
/// proyecto declarado por <c>ProyectoId</c> o por URL del tailoring, sin
/// extraer entries de tiempo ni tasks (alineado con <c>flujo_auditoria.md</c>
/// Paso 6).
/// </summary>
[McpServerToolType]
public sealed class ClockifyMcpTools
{
    private readonly ClockifyApiClient _api;
    private readonly IProyectoRepository _proyectos;
    private readonly IClockifyWorkspaceResolver _workspace;
    private readonly ILogger<ClockifyMcpTools> _logger;

    public ClockifyMcpTools(
        ClockifyApiClient api,
        IProyectoRepository proyectos,
        IClockifyWorkspaceResolver workspace,
        ILogger<ClockifyMcpTools> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _proyectos = proyectos ?? throw new ArgumentNullException(nameof(proyectos));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [McpServerTool(Name = "get_project_summary")]
    [Description(
        "Devuelve el resumen del proyecto de Clockify asociado al ProyectoId " +
        "(via proyecto.clockify_project_id + clockify_workspace_id global). " +
        "Valida existencia: si el proyecto no tiene clockify_project_id o el " +
        "proyecto no existe en Clockify, devuelve error con detalle.")]
    public async Task<ClockifyProjectSummary> GetProjectSummaryAsync(
        [Description("Identificador del proyecto en la base de datos del sistema.")]
        int proyectoId,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "get_project_summary";

        try
        {
            var proyecto = await _proyectos
                .GetByIdAsync(proyectoId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"No existe proyecto activo con Id={proyectoId}.");

            if (string.IsNullOrWhiteSpace(proyecto.ClockifyProjectId))
            {
                throw new InvalidOperationException(
                    $"El proyecto {proyectoId} no tiene 'clockify_project_id' configurado.");
            }

            var workspaceId = await _workspace
                .ResolveAsync(cancellationToken).ConfigureAwait(false);

            return await _api
                .GetProjectAsync(workspaceId, proyecto.ClockifyProjectId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invocando MCP tool '{ToolName}' con ProyectoId={ProyectoId}.",
                toolName, proyectoId);
            throw ex.AsToolError(toolName);
        }
    }

    [McpServerTool(Name = "get_project_by_url")]
    [Description(
        "Devuelve el resumen del proyecto de Clockify identificado por la URL " +
        "indicada. Extrae automáticamente el projectId y el workspaceId si la " +
        "URL los contiene (formato /workspaces/{ws}/projects/{id}); si no, usa " +
        "el clockify_workspace_id global del sistema.")]
    public async Task<ClockifyProjectSummary> GetProjectByUrlAsync(
        [Description("URL https://app.clockify.me/... del proyecto.")]
        string clockifyUrl,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "get_project_by_url";

        try
        {
            var info = ClockifyProjectUrl.TryExtract(clockifyUrl)
                ?? throw new InvalidOperationException(
                    "La URL no contiene un identificador de proyecto de Clockify reconocible " +
                    "(espere /projects/{id}, /workspaces/{ws}/projects/{id} o /tracker?project={id}).");

            var workspaceId = info.WorkspaceId
                              ?? await _workspace
                                  .ResolveAsync(cancellationToken)
                                  .ConfigureAwait(false);

            return await _api
                .GetProjectAsync(workspaceId, info.ProjectId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invocando MCP tool '{ToolName}' con clockifyUrl.",
                toolName);
            throw ex.AsToolError(toolName);
        }
    }
}
