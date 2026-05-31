// ============================================================================
//  DriveMcpExtensions — Wiring de DI para server + cliente MCP de Drive
// ----------------------------------------------------------------------------
//  Una sola extensión: AddGoogleDriveMcpServer. Registra:
//   - GoogleDriveOptions bindeado a "GoogleDrive".
//   - GoogleDriveClient como Singleton (Lazy<DriveService> internamente).
//   - El server MCP con HttpTransport stateless y las DriveMcpTools.
//   - McpDriveClientOptions bindeado a "Mcp:Drive" (D3.2).
//   - DriveMcpSdkClient como Singleton, implementando IDriveMcpClient (D3.2).
//
//  El montaje del endpoint /mcp/drive sigue en Program.cs con app.MapMcp.
// ============================================================================

using ISOAuditAgent.API.Agents.DocumentAnalysis;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Drive;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;

namespace ISOAuditAgent.API.Integrations.MCP.Drive;

public static class DriveMcpExtensions
{
    public static IServiceCollection AddGoogleDriveMcpServer(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // --- Server side (D3.1) ---
        services.Configure<GoogleDriveOptions>(
            configuration.GetSection(GoogleDriveOptions.SectionName));

        services.AddSingleton<GoogleDriveClient>();

        services
            .AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithTools<DriveMcpTools>();

        // --- Client side (D3.2) ---
        services.Configure<McpDriveClientOptions>(
            configuration.GetSection(McpDriveClientOptions.SectionName));

        // Singleton: una conexión MCP reusada en todo el proceso. La conexión
        // se crea perezosamente en el primer call (ver DriveMcpSdkClient).
        services.AddSingleton<IDriveMcpClient, DriveMcpSdkClient>();

        // --- Tailoring (D3.4) ---
        // TailoringReader es interno al agente; TailoringSource es la API
        // pública que consume DocumentAnalysisNode. Ambos Scoped.
        services.AddScoped<TailoringReader>();
        services.AddScoped<ITailoringSource, TailoringSource>();

        // --- Checker físico de artefactos (D3.5) ---
        // Scoped: una instancia por scope de DI (que matchea el scope del nodo
        // del workflow). Sin estado propio — la conexión MCP que usa es Singleton.
        services.AddScoped<IArtefactoFisicoChecker, ArtefactoFisicoChecker>();

        return services;
    }
}