using ISOAuditAgent.API.Mcp.Trello;
using ISOAuditAgent.Infrastructure.Entities;
using ISOAuditAgent.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol;

namespace ISOAuditAgent.API.IntegrationTests.Mcp.Trello;

/// <summary>
/// Verifica que <see cref="TrelloMcpTools"/> capture excepciones de
/// validación previas a la llamada HTTP y las relance como
/// <see cref="McpException"/> con un mensaje útil. Las pruebas que
/// requieren credenciales reales viven aparte (manuales / integración).
/// </summary>
public sealed class TrelloMcpToolsErrorVisibilityTests
{
    [Fact]
    public async Task GetProjectBoard_ProyectoInexistente_DevuelveMcpException()
    {
        var tools = BuildTools(new StubProyectoRepository(proyecto: null));

        var ex = await Assert.ThrowsAsync<McpException>(
            () => tools.GetProjectBoardAsync(proyectoId: 999));

        Assert.Contains("get_project_board", ex.Message);
        Assert.Contains("Id=999", ex.Message);
    }

    [Fact]
    public async Task GetProjectBoard_ProyectoSinTrelloBoardId_DevuelveMcpException()
    {
        var proyecto = new Proyecto
        {
            Id = 1,
            Nombre = "Sin Trello",
            TrelloBoardId = null
        };
        var tools = BuildTools(new StubProyectoRepository(proyecto));

        var ex = await Assert.ThrowsAsync<McpException>(
            () => tools.GetProjectBoardAsync(proyectoId: 1));

        Assert.Contains("get_project_board", ex.Message);
        Assert.Contains("trello_board_id", ex.Message);
    }

    [Fact]
    public async Task GetBoardByUrl_UrlNoReconocible_DevuelveMcpException()
    {
        var tools = BuildTools(new StubProyectoRepository(proyecto: null));

        var ex = await Assert.ThrowsAsync<McpException>(
            () => tools.GetBoardByUrlAsync("https://otra.com/cosa"));

        Assert.Contains("get_board_by_url", ex.Message);
        Assert.Contains("board de Trello", ex.Message);
    }

    private static TrelloMcpTools BuildTools(IProyectoRepository proyectos)
    {
        var apiOptions = Options.Create(new TrelloOptions());
        // HttpClient sin handler: no se va a invocar porque las pruebas
        // fallan antes (validación de input + credenciales).
        var api = new TrelloApiClient(
            new HttpClient { BaseAddress = new Uri("https://api.trello.com/1/") },
            apiOptions,
            NullLogger<TrelloApiClient>.Instance);

        return new TrelloMcpTools(api, proyectos, NullLogger<TrelloMcpTools>.Instance);
    }

    private sealed class StubProyectoRepository : IProyectoRepository
    {
        private readonly Proyecto? _proyecto;

        public StubProyectoRepository(Proyecto? proyecto) => _proyecto = proyecto;

        public Task<Proyecto?> GetByIdAsync(int proyectoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_proyecto);
    }
}
