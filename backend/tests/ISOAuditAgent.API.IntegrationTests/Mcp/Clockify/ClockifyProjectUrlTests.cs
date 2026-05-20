using ISOAuditAgent.API.Mcp.Clockify;

namespace ISOAuditAgent.API.IntegrationTests.Mcp.Clockify;

/// <summary>
/// Verifica la extracción de projectId / workspaceId de URLs típicas
/// de Clockify.
/// </summary>
public sealed class ClockifyProjectUrlTests
{
    [Fact]
    public void URL_simple_de_proyecto_extrae_solo_projectId()
    {
        var info = ClockifyProjectUrl.TryExtract(
            "https://app.clockify.me/projects/507f1f77bcf86cd799439011");
        Assert.NotNull(info);
        Assert.Equal("507f1f77bcf86cd799439011", info!.ProjectId);
        Assert.Null(info.WorkspaceId);
    }

    [Fact]
    public void URL_completa_extrae_workspaceId_y_projectId()
    {
        var info = ClockifyProjectUrl.TryExtract(
            "https://app.clockify.me/workspaces/ws12345678/projects/proj98765432");
        Assert.NotNull(info);
        Assert.Equal("proj98765432", info!.ProjectId);
        Assert.Equal("ws12345678", info.WorkspaceId);
    }

    [Fact]
    public void URL_tipo_tracker_query_extrae_projectId()
    {
        var info = ClockifyProjectUrl.TryExtract(
            "https://app.clockify.me/tracker?project=507f1f77bcf86cd799439011&user=abc");
        Assert.NotNull(info);
        Assert.Equal("507f1f77bcf86cd799439011", info!.ProjectId);
        Assert.Null(info.WorkspaceId);
    }

    [Theory]
    [InlineData("https://app.clockify.me/")]
    [InlineData("https://app.clockify.me/tracker")] // sin ?project=
    [InlineData("https://app.clockify.me/projects/short")] // <8 chars
    public void Devuelve_null_si_no_hay_projectId(string url)
    {
        Assert.Null(ClockifyProjectUrl.TryExtract(url));
    }
}
