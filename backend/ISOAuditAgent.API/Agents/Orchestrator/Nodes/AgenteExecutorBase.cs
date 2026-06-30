
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace ISOAuditAgent.API.Agents.Orchestrator;

/// <summary>
/// Base de los nodos del workflow que envuelven un AIAgent. TInput y TOutput
/// son los DTOs de contrato (contratos_agentes.md). El executor es tipado:
/// el grafo transporta DTOs, no ChatMessage.
/// </summary>
public abstract class AgenteExecutorBase<TInput, TOutput>
    : Executor<TInput, TOutput>
{
    private const int MaxIntentos = 3;

    /// <summary>El AIAgent que razona. Detalle de implementación interno.</summary>
    protected AIAgent Agente { get; }

    protected AgenteExecutorBase(string executorId, AIAgent agente)
        : base(executorId)
    {
        Agente = agente;
    }

    /// <summary>
    /// Esqueleto fijo del nodo-agente (template method). Los nodos concretos
    /// no overridean esto; implementan ConstruirPrompt y ParsearRespuesta.
    /// </summary>
    public override async ValueTask<TOutput> HandleAsync(
        TInput message, IWorkflowContext context, CancellationToken ct = default)
    {
        // 1. DTO -> prompt
        string prompt = ConstruirPrompt(message);

        // 2 + 3. Llamada al LLM con reintentos para tolerar timeouts de red o
        // demoras de modelos de razonamiento (gemini-2.5-flash, etc.).
        Exception? ultimoError = null;
        for (int intento = 1; intento <= MaxIntentos; intento++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var respuesta = await Agente.RunAsync(prompt, cancellationToken: ct);
                string textoRespuesta = respuesta.Text ?? string.Empty;

                // 4. Respuesta del LLM -> DTO de salida
                return ParsearRespuesta(message, textoRespuesta);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested && intento < MaxIntentos)
            {
                ultimoError = ex;
            }
        }
        throw ultimoError!;
    }

    /// <summary>
    /// Construye el prompt del LLM a partir del DTO de entrada. Cada nodo lo
    /// implementa según su tarea. Acá vive la instrucción "actuá como un
    /// auditor de ISO 9001" y el contenido concreto a analizar.
    /// </summary>
    protected abstract string ConstruirPrompt(TInput input);

    /// <summary>
    /// Convierte la respuesta textual del LLM en el DTO de salida tipado.
    /// Recibe también el input original porque el DTO de salida suele
    /// necesitar IDs que vienen del input (AuditoriaId, etc.), no del LLM.
    /// </summary>
    protected abstract TOutput ParsearRespuesta(TInput input, string textoLlm);
}
