// ============================================================================
//  TrelloMcpExtensions — Wiring de DI para server + cliente MCP de Trello
// ----------------------------------------------------------------------------
//  Replica exactamente el patrón de DriveMcpExtensions:
//   - TrelloOptions → sección "Trello" (ApiKey, Token).
//   - TrelloApiClient como Singleton.
//   - Server MCP con HttpTransport stateless y TrelloMcpTools.
//   - McpTrelloClientOptions → sección "Mcp:Trello".
//   - ITrelloMcpClient (TrelloMcpSdkClient) como Singleton.
//
//  El montaje del endpoint /mcp/trello sigue en Program.cs con app.MapMcp.
// ============================================================================

using ISOAuditAgent.API.Agents.DocumentAnalysis.Trello;

namespace ISOAuditAgent.API.Integrations.MCP.Trello;

public static class TrelloMcpExtensions
{
    public static IServiceCollection AddTrelloMcpServer(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // --- Server side ---
        services.Configure<TrelloOptions>(
            configuration.GetSection(TrelloOptions.SectionName));

        services.AddSingleton<TrelloApiClient>();

        services
            .AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithTools<TrelloMcpTools>();

        // --- Client side ---
        services.Configure<McpTrelloClientOptions>(
            configuration.GetSection(McpTrelloClientOptions.SectionName));

        services.AddSingleton<ITrelloMcpClient, TrelloMcpSdkClient>();

        // --- Checker de artefactos Trello ---
        services.AddScoped<TrelloChecker>();

        return services;
    }
}
