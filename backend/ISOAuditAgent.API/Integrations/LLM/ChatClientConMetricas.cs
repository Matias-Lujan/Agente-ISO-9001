using System.Runtime.CompilerServices;

using Microsoft.Extensions.AI;

using ISOAuditAgent.API.Agents.Orchestrator;

namespace ISOAuditAgent.API.Integrations.LLM;

/// <summary>
/// Decorador de IChatClient que mide el consumo de tokens de cada llamada al
/// LLM. Envuelve el cliente real de Gemini y registra en el ColectorConsumoTokens
/// del scope el Usage que reporta cada respuesta (Gemini devuelve usageMetadata).
///
/// Se aplica UNA vez por AIAgent en el factory de DI: como cada agente tiene su
/// propio wrapper con su AgenteKey, la atribución del consumo por agente sale
/// sin necesidad de contexto ambiental. La AuditoriaId no la conoce este wrapper;
/// la estampa el AuditoriaRunner al drenar el colector.
///
/// Es defensivo por diseño: si Usage viene null (el provider no lo reportó) o si
/// el registro fallara, no interfiere con la respuesta del LLM.
/// </summary>
internal sealed class ChatClientConMetricas : DelegatingChatClient
{
    private readonly string _agenteKey;
    private readonly string _modelo;
    private readonly ColectorConsumoTokens _colector;

    public ChatClientConMetricas(
        IChatClient inner, string agenteKey, string modelo, ColectorConsumoTokens colector)
        : base(inner)
    {
        _agenteKey = agenteKey;
        _modelo = modelo;
        _colector = colector;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var respuesta = await base.GetResponseAsync(messages, options, cancellationToken)
            .ConfigureAwait(false);

        Registrar(respuesta.Usage);
        return respuesta;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // El path no-streaming es el que usa AIAgent.RunAsync, pero cubrimos el
        // streaming por completitud: el Usage llega como UsageContent en uno de
        // los updates (habitualmente el último).
        UsageDetails? usage = null;

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken)
            .ConfigureAwait(false))
        {
            foreach (var contenido in update.Contents)
            {
                if (contenido is UsageContent uc)
                    usage = uc.Details;
            }

            yield return update;
        }

        Registrar(usage);
    }

    private void Registrar(UsageDetails? usage)
    {
        if (usage is null)
            return;

        var entrada = usage.InputTokenCount ?? 0;
        var salida = usage.OutputTokenCount ?? 0;
        // Si el provider no manda total, lo derivamos de entrada + salida.
        var total = usage.TotalTokenCount ?? (entrada + salida);

        if (entrada == 0 && salida == 0 && total == 0)
            return;

        _colector.Registrar(new RegistroConsumo(_agenteKey, _modelo, entrada, salida, total));
    }
}
