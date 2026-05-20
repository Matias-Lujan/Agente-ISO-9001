using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;

namespace ISOAuditAgent.API.Mcp.Trello;

/// <summary>
/// Extensiones del host para registrar el <see cref="TrelloApiClient"/>
/// (vía <c>IHttpClientFactory</c>) y exponer <see cref="TrelloMcpTools"/>
/// al servidor MCP compartido del host (Fase F.5.1).
/// </summary>
/// <remarks>
/// Por simplicidad del SDK MAF 1.2 (un solo servidor MCP por host), las
/// tools de Trello se acumulan en el mismo servidor que ya registra
/// <c>DriveMcpTools</c>. El cliente las distingue por el nombre de tool
/// (<c>get_project_board</c>, <c>get_board_by_url</c>).
/// </remarks>
public static class TrelloMcpExtensions
{
    /// <summary>
    /// Registra <see cref="TrelloOptions"/>, <see cref="TrelloApiClient"/>
    /// (HttpClient tipado) y suma <see cref="TrelloMcpTools"/> al servidor
    /// MCP del host.
    /// </summary>
    /// <remarks>
    /// Si <c>DriveMcpExtensions.AddGoogleDriveMcpServer</c> no fue invocado
    /// antes, este método igual registra un <c>AddMcpServer</c> básico — el
    /// SDK admite invocaciones idempotentes y acumula tools.
    /// </remarks>
    public static IServiceCollection AddTrelloMcpTools(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<TrelloOptions>()
            .Bind(configuration.GetSection(TrelloOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddHttpClient<TrelloApiClient>();

        services
            .AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithTools<TrelloMcpTools>();

        return services;
    }
}
