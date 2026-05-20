using ISOAuditAgent.API.Agents.FindingsClassification.Agents;
using ISOAuditAgent.API.Agents.FindingsClassification.Executors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mscc.GenerativeAI.Microsoft;

namespace ISOAuditAgent.API.Agents.FindingsClassification.Extensions;

/// <summary>
/// Registro del agente FindingsClassification en el contenedor de DI del host.
/// </summary>
public static class ServiceCollectionExtensions
{
    public const string ConfigSectionKey = "Gemini";
    public const string ApiKeyConfigKey = "Gemini:ApiKey";
    public const string ApiKeyEnvironmentVariable = "GEMINI_API_KEY";
    public const string DefaultModel = "gemini-2.5-flash";

    /// <summary>
    /// Registra el agente FindingsClassification.
    /// </summary>
    /// <remarks>
    /// Lee la API key de Gemini desde la configuración (<c>Gemini:ApiKey</c>) o la
    /// variable de entorno <c>GEMINI_API_KEY</c>. Falla en startup si no se
    /// encuentra — el agente no puede operar sin LLM.
    /// </remarks>
    public static IServiceCollection AddFindingsClassification(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<FindingsClassificationAgent>(_ =>
        {
            var apiKey = configuration[ApiKeyConfigKey];
            if (string.IsNullOrWhiteSpace(apiKey))
                apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable);

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException(
                    "Gemini API key no configurada para FindingsClassification. " +
                    $"Definí '{ApiKeyConfigKey}' en appsettings o '{ApiKeyEnvironmentVariable}' en el entorno.");

            var geminiClient = new GeminiChatClient(apiKey: apiKey, model: DefaultModel);
            var agent = AgentFactory.CreateFindingsClassificationAgent(geminiClient);
            return new FindingsClassificationAgent(agent);
        });

        services.AddScoped<FindingsClassificationExecutor>();
        return services;
    }
}
