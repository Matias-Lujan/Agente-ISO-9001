using ISOAuditAgent.API.Agents.DocumentAnalysis.Clockify;

namespace ISOAuditAgent.API.Integrations.MCP.Clockify;

public static class ClockifyMcpExtensions
{
    public static IServiceCollection AddClockifyMcpServer(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // --- Server side ---
        services.Configure<ClockifyOptions>(
            configuration.GetSection(ClockifyOptions.SectionName));

        services.AddSingleton<ClockifyApiClient>();

        services
            .AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithTools<ClockifyMcpTools>();

        // --- Client side ---
        services.Configure<McpClockifyClientOptions>(
            configuration.GetSection(McpClockifyClientOptions.SectionName));

        services.AddSingleton<IClockifyMcpClient, ClockifyMcpSdkClient>();

        // --- Checker de artefactos Clockify ---
        services.AddScoped<ClockifyChecker>();

        return services;
    }
}
