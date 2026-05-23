// ============================================================================
//  GeminiServiceCollectionExtensions — Wiring de Gemini en DI (D4)
// ----------------------------------------------------------------------------
//  Una extensión: AddGeminiAndAgents.
//
//  Registra:
//   - GeminiOptions bindeado a "Gemini".
//   - IChatClient Singleton: la conexión real con Gemini. El nodo NO ve
//     "GeminiChatClient"; solo IChatClient. Cambiar provider = cambiar este
//     método, sin tocar nodos.
//   - 4 AIAgent como keyed Singletons (uno por nodo LLM). Cada uno con su
//     SystemPrompts.<NodoName> como instructions.
//
//  ALCANCE D4:
//   - NO registra los nodos del workflow. Eso es D5.
//   - NO arma el workflow. Eso es D5.
//   - Solo deja IChatClient y 4 AIAgent disponibles en DI para que los
//     smokes /api/_smoke/llm y /api/_smoke/agent validen la conexión real
//     contra Gemini.
//
//  KEYS DE LOS AIAGENT:
//   "DocumentAnalysis", "ComplianceValidation", "ConsistencyVerification",
//   "FindingsClassification". Coinciden con los nombres de carpeta de cada
//   agente bajo Agents/ y con el nombre del SystemPrompts correspondiente.
// ============================================================================

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Mscc.GenerativeAI.Microsoft;

using Microsoft.Extensions.Options;

namespace ISOAuditAgent.API.Integrations.LLM;

public static class GeminiServiceCollectionExtensions
{
    public static IServiceCollection AddGeminiAndAgents(
        this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // 1. Bind de GeminiOptions
        services.Configure<GeminiOptions>(
            configuration.GetSection(GeminiOptions.SectionName));

        // 2. IChatClient Singleton — la conexión real con Gemini.
        //
        // Mscc.GenerativeAI.Microsoft 3.1.0 expone GeminiChatClient con un
        // constructor que toma apiKey + model. .AsIChatClient(modelId)
        // devuelve la implementación IChatClient compatible con MAF.
        //
        // Singleton: la doc de Microsoft.Extensions.AI requiere que IChatClient
        // sea seguro para uso concurrente. Si en runtime aparece un problema
        // de thread-safety con este provider, se baja a Scoped.
        services.AddSingleton<IChatClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;

            if (string.IsNullOrWhiteSpace(opts.ApiKey))
            {
                throw new InvalidOperationException(
                    "Gemini:ApiKey no está configurada. Setear con: " +
                    "dotnet user-secrets set \"Gemini:ApiKey\" \"<tu-api-key>\"");
            }

            // Construcción del cliente Gemini real, expuesto como IChatClient.
            // Si la firma del paquete cambia en una versión menor, este es el
            // único punto a tocar
            return new GeminiChatClient(apiKey: opts.ApiKey, model: opts.ModelId);
        });

        // 3. 4 AIAgent keyed, uno por nodo LLM.
        //
        // Cada AIAgent comparte el mismo IChatClient pero tiene instructions
        // distintas (su SystemPrompt). MAF maneja la conversación con esas
        // instructions como contexto del sistema en cada call.
        services.AddKeyedSingleton<AIAgent>("DocumentAnalysis", (sp, _) =>
        {
            var chat = sp.GetRequiredService<IChatClient>();
            return new ChatClientAgent(
                chat,
                instructions: ISOAuditAgent.API.Agents.DocumentAnalysis.SystemPrompts.AnalizadorDocumental);
        });

        services.AddKeyedSingleton<AIAgent>("ComplianceValidation", (sp, _) =>
        {
            var chat = sp.GetRequiredService<IChatClient>();
            return new ChatClientAgent(
                chat,
                instructions: "Sos el agente ComplianceValidation. Validás tailoring/procedimiento contra ejecución real.");
        });

        services.AddKeyedSingleton<AIAgent>("ConsistencyVerification", (sp, _) =>
        {
            var chat = sp.GetRequiredService<IChatClient>();
            return new ChatClientAgent(
                chat,
                instructions: "Sos el agente ConsistencyVerification. Validás consistencia formal y estructural entre artefactos.");
        });

        services.AddKeyedSingleton<AIAgent>("FindingsClassification", (sp, _) =>
        {
            var chat = sp.GetRequiredService<IChatClient>();
            return new ChatClientAgent(
                chat,
                instructions: "Sos el agente FindingsClassification. Clasificás hallazgos en NC, OBS u OM.");
        });

        return services;
    }
}