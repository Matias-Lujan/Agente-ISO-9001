using ISOAuditAgent.API.Mcp.Clockify;
using ISOAuditAgent.Infrastructure.Entities;
using ISOAuditAgent.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol;

namespace ISOAuditAgent.API.IntegrationTests.Mcp.Clockify;

/// <summary>
/// Verifica que <see cref="ClockifyMcpTools"/> capture excepciones de
/// validación previas a la llamada HTTP y las relance como
/// <see cref="McpException"/> con mensajes útiles.
/// </summary>
public sealed class ClockifyMcpToolsErrorVisibilityTests
{
    [Fact]
    public async Task GetProjectSummary_ProyectoInexistente_DevuelveMcpException()
    {
        var tools = BuildTools(
            new StubProyectoRepository(null),
            new StubWorkspaceResolver("ws-default"));

        var ex = await Assert.ThrowsAsync<McpException>(
            () => tools.GetProjectSummaryAsync(proyectoId: 999));

        Assert.Contains("get_project_summary", ex.Message);
        Assert.Contains("Id=999", ex.Message);
    }

    [Fact]
    public async Task GetProjectSummary_ProyectoSinClockifyProjectId_DevuelveMcpException()
    {
        var proyecto = new Proyecto
        {
            Id = 1,
            Nombre = "Sin Clockify",
            ClockifyProjectId = null
        };
        var tools = BuildTools(
            new StubProyectoRepository(proyecto),
            new StubWorkspaceResolver("ws-default"));

        var ex = await Assert.ThrowsAsync<McpException>(
            () => tools.GetProjectSummaryAsync(proyectoId: 1));

        Assert.Contains("get_project_summary", ex.Message);
        Assert.Contains("clockify_project_id", ex.Message);
    }

    [Fact]
    public async Task GetProjectByUrl_UrlNoReconocible_DevuelveMcpException()
    {
        var tools = BuildTools(
            new StubProyectoRepository(null),
            new StubWorkspaceResolver("ws-default"));

        var ex = await Assert.ThrowsAsync<McpException>(
            () => tools.GetProjectByUrlAsync("https://otra.com/cosa"));

        Assert.Contains("get_project_by_url", ex.Message);
        Assert.Contains("proyecto de Clockify", ex.Message);
    }

    [Fact]
    public async Task GetProjectByUrl_UrlValidaSinWorkspaceConfigurado_DevuelveMcpException()
    {
        var tools = BuildTools(
            new StubProyectoRepository(null),
            new ThrowingWorkspaceResolver(new InvalidOperationException(
                "Clockify: no se pudo resolver el workspaceId.")));

        var ex = await Assert.ThrowsAsync<McpException>(
            () => tools.GetProjectByUrlAsync(
                "https://app.clockify.me/projects/507f1f77bcf86cd799439011"));

        Assert.Contains("get_project_by_url", ex.Message);
        Assert.Contains("workspaceId", ex.Message);
    }

    private static ClockifyMcpTools BuildTools(
        IProyectoRepository proyectos,
        IClockifyWorkspaceResolver workspace)
    {
        var apiOptions = Options.Create(new ClockifyOptions());
        // HttpClient sin handler real; las pruebas fallan antes de invocarlo.
        var api = new ClockifyApiClient(
            new HttpClient { BaseAddress = new Uri("https://api.clockify.me/api/v1/") },
            apiOptions,
            NullLogger<ClockifyApiClient>.Instance);

        return new ClockifyMcpTools(api, proyectos, workspace, NullLogger<ClockifyMcpTools>.Instance);
    }

    private sealed class StubProyectoRepository : IProyectoRepository
    {
        private readonly Proyecto? _proyecto;

        public StubProyectoRepository(Proyecto? proyecto) => _proyecto = proyecto;

        public Task<Proyecto?> GetByIdAsync(int proyectoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_proyecto);
    }

    private sealed class StubWorkspaceResolver : IClockifyWorkspaceResolver
    {
        private readonly string _workspaceId;

        public StubWorkspaceResolver(string workspaceId) => _workspaceId = workspaceId;

        public Task<string> ResolveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_workspaceId);
    }

    private sealed class ThrowingWorkspaceResolver : IClockifyWorkspaceResolver
    {
        private readonly Exception _ex;

        public ThrowingWorkspaceResolver(Exception ex) => _ex = ex;

        public Task<string> ResolveAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<string>(_ex);
    }
}
