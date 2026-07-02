// ============================================================================
//  GeminiServiceCollectionExtensions � Wiring de Gemini en DI (D4)
// ----------------------------------------------------------------------------
//  Una extensi�n: AddGeminiAndAgents.
//
//  Registra:
//   - GeminiOptions bindeado a "Gemini".
//   - IChatClient Singleton: la conexi�n real con Gemini. El nodo NO ve
//     "GeminiChatClient"; solo IChatClient. Cambiar provider = cambiar este
//     m�todo, sin tocar nodos.
//   - 4 AIAgent como keyed Singletons (uno por nodo LLM). Cada uno con su
//     SystemPrompts.<NodoName> como instructions.
//
//  ALCANCE D4:
//   - NO registra los nodos del workflow. Eso es D5.
//   - NO arma el workflow. Eso es D5.
//   - Solo deja IChatClient y 4 AIAgent disponibles en DI para que los
//     smokes /api/_smoke/llm y /api/_smoke/agent validen la conexi�n real
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

using ISOAuditAgent.API.Agents.DocumentAnalysis;
using ISOAuditAgent.API.Agents.Orchestrator;
using ISOAuditAgent.API.Services;

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

        // 2. IChatClient Singleton � la conexi�n real con Gemini.
        //
        // Mscc.GenerativeAI.Microsoft 3.1.0 expone GeminiChatClient con un
        // constructor que toma apiKey + model. .AsIChatClient(modelId)
        // devuelve la implementaci�n IChatClient compatible con MAF.
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
                    "Gemini:ApiKey no est� configurada. Setear con: " +
                    "dotnet user-secrets set \"Gemini:ApiKey\" \"<tu-api-key>\"");
            }

            // Construcci�n del cliente Gemini real, expuesto como IChatClient.
            // Si la firma del paquete cambia en una versi�n menor, este es el
            // �nico punto a tocar
            return new GeminiChatClient(apiKey: opts.ApiKey, model: opts.ModelId);
        });

        // 2.b Colector de consumo de tokens — Scoped: uno por auditoría. Lo
        // comparten los 4 AIAgent de ese scope (vía ChatClientConMetricas) y lo
        // drena el AuditoriaRunner al terminar para persistir el consumo.
        services.AddScoped<ColectorConsumoTokens>();

        // 3. 4 AIAgent keyed, uno por nodo LLM.
        //
        // Cada AIAgent comparte el mismo IChatClient pero tiene instructions
        // distintas (su system prompt). El prompt ya NO se hornea acá: se lee del
        // IPromptStore (versión activa en BD, con fallback al default en código).
        //
        // Por eso son Scoped, no Singleton: cada auditoría corre en su propio
        // scope (AuditoriaRunner.CreateScope) y resuelve estos agentes ahí, así
        // toma el prompt guardado más reciente sin reiniciar el backend. Los
        // únicos consumidores son los nodos (también Scoped): no hay captura de
        // dependencia scoped por un singleton.
        foreach (var agenteKey in SystemPrompts.Defaults.Keys)
        {
            var key = agenteKey; // captura por iteración
            services.AddKeyedScoped<AIAgent>(key, (sp, _) =>
            {
                var chat = sp.GetRequiredService<IChatClient>();
                var prompts = sp.GetRequiredService<IPromptStore>();

                // Decorar el chat client para medir el consumo de tokens de este
                // agente. El wrapper conoce su key -> atribución por agente sin
                // contexto ambiental. El modelo sale de la config de Gemini.
                var modelo = sp.GetRequiredService<IOptions<GeminiOptions>>().Value.ModelId;
                var colector = sp.GetRequiredService<ColectorConsumoTokens>();
                var chatMedido = new ChatClientConMetricas(chat, key, modelo, colector);

                return new ChatClientAgent(chatMedido, instructions: prompts.ObtenerActivo(key));
            });
        }

        return services;
    }
}