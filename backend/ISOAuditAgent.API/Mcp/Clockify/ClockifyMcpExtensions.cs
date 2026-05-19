using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;

namespace ISOAuditAgent.API.Mcp.Clockify;

/// <summary>
/// Extensiones del host para registrar el <see cref="ClockifyApiClient"/>,
/// el <see cref="IClockifyWorkspaceResolver"/> y sumar
/// <see cref="ClockifyMcpTools"/> al servidor MCP del host (Fase F.5.2).
/// </summary>
/// <remarks>
/// Comparte el servidor MCP único del host (mismo path que Drive/Trello).
/// Las tools se distinguen por nombre (<c>get_project_summary</c>,
/// <c>get_project_by_url</c>).
/// </remarks>
public static class ClockifyMcpExtensions
{
    public static IServiceCollection AddClockifyMcpTools(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<ClockifyOptions>()
            .Bind(configuration.GetSection(ClockifyOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddHttpClient<ClockifyApiClient>();
        services.AddScoped<IClockifyWorkspaceResolver, ClockifyWorkspaceResolver>();

        services
            .AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithTools<ClockifyMcpTools>();

        return services;
    }
}
