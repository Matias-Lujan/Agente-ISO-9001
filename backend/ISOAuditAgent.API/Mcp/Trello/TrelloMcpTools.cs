using System.ComponentModel;
using ISOAuditAgent.DocumentAnalysis.Mcp.Trello;
using ISOAuditAgent.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ISOAuditAgent.API.Mcp.Trello;

/// <summary>
/// Herramientas MCP de Trello (Fase F.5.1). Validan existencia del board
/// declarado por proyecto o por URL del tailoring, sin extraer tarjetas
/// ni contenido (alineado con <c>flujo_auditoria.md</c> Paso 6: el sistema
/// valida existencia y estructura, no contenido).
/// </summary>
/// <remarks>
/// Las tools se exponen en el mismo servidor MCP del host. El cliente del
/// agente las invoca por nombre (<c>get_project_board</c> / <c>get_board_by_url</c>);
/// no comparten path con las de Drive a nivel lógico, pero sí a nivel
/// transporte por simplicidad del SDK MAF.
/// </remarks>
[McpServerToolType]
public sealed class TrelloMcpTools
{
    private readonly TrelloApiClient _api;
    private readonly IProyectoRepository _proyectos;
    private readonly ILogger<TrelloMcpTools> _logger;

    public TrelloMcpTools(
        TrelloApiClient api,
        IProyectoRepository proyectos,
        ILogger<TrelloMcpTools> logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _proyectos = proyectos ?? throw new ArgumentNullException(nameof(proyectos));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [McpServerTool(Name = "get_project_board")]
    [Description(
        "Devuelve el resumen del board de Trello asociado al ProyectoId " +
        "(via proyecto.trello_board_id). Valida existencia: si el proyecto " +
        "no tiene trello_board_id configurado o el board no existe en Trello, " +
        "devuelve error con detalle.")]
    public async Task<TrelloBoardSummary> GetProjectBoardAsync(
        [Description("Identificador del proyecto en la base de datos del sistema.")]
        int proyectoId,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "get_project_board";

        try
        {
            var proyecto = await _proyectos
                .GetByIdAsync(proyectoId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"No existe proyecto activo con Id={proyectoId}.");

            if (string.IsNullOrWhiteSpace(proyecto.TrelloBoardId))
            {
                throw new InvalidOperationException(
                    $"El proyecto {proyectoId} no tiene 'trello_board_id' configurado.");
            }

            return await _api
                .GetBoardAsync(proyecto.TrelloBoardId, cancellationToken)
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

    [McpServerTool(Name = "get_board_by_url")]
    [Description(
        "Devuelve el resumen del board de Trello identificado por la URL " +
        "indicada (extrae automáticamente el shortLink o boardId de URLs " +
        "del tipo https://trello.com/b/...).")]
    public async Task<TrelloBoardSummary> GetBoardByUrlAsync(
        [Description("URL https://trello.com/b/... del board.")]
        string trelloUrl,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "get_board_by_url";

        try
        {
            var boardRef = TrelloBoardUrl.TryExtractBoardIdOrShortLink(trelloUrl);
            if (string.IsNullOrEmpty(boardRef))
            {
                throw new InvalidOperationException(
                    "La URL no contiene un identificador de board de Trello reconocible " +
                    "(espere /b/{id-o-shortLink}/...).");
            }

            return await _api
                .GetBoardAsync(boardRef, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invocando MCP tool '{ToolName}' con trelloUrl.",
                toolName);
            throw ex.AsToolError(toolName);
        }
    }
}
