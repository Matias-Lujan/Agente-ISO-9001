using ISOAuditAgent.DocumentAnalysis.Drive;
using ModelContextProtocol.Server;

namespace ISOAuditAgent.API.Mcp.Drive;

/// <summary>
/// Extensiones del host para registrar el cliente real de Google Drive
/// (<see cref="GoogleDriveClient"/>) y el servidor MCP que expone
/// <see cref="DriveMcpTools"/>.
/// </summary>
public static class DriveMcpExtensions
{
    /// <summary>
    /// Registra <see cref="IDriveClient"/> → <see cref="GoogleDriveClient"/>
    /// y el servidor MCP HTTP (stateless) con las herramientas de Drive.
    /// </summary>
    public static IServiceCollection AddGoogleDriveMcpServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IDriveClient, GoogleDriveClient>();

        services
            .AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithTools<DriveMcpTools>();

        return services;
    }
}
