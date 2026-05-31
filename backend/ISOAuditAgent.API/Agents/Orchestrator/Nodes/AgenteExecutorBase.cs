// ============================================================================
//  AgenteExecutorBase — Patrón común de los 4 nodos-agente del workflow
// ----------------------------------------------------------------------------
//  Los 4 agentes especializados (DocumentAnalysis, ComplianceValidation,
//  ConsistencyVerification, FindingsClassification) NO van al grafo como
//  AIAgent crudos.
//
//  Razón: cuando se agrega un AIAgent directo a un workflow, MAF lo envuelve
//  en un host executor que normaliza el input a IList<ChatMessage>. Eso sirve
//  para agentes-pasándose-texto, pero nuestros contratos son DTOs tipados
//  fuertes (ContextoAuditoria, DocumentosExtraidos, etc.). Para que entre un
//  DTO y salga un DTO, cada agente va envuelto en un Executor<TIn, TOut>
//  custom: el AIAgent es un campo privado, un detalle de implementación.
//
//  El HandleAsync de cada nodo-agente sigue siempre los mismos 4 pasos:
//    1. Recibe el DTO de entrada.
//    2. Construye el prompt a partir del DTO (ConstruirPrompt).
//    3. Llama al LLM vía el AIAgent.
//    4. Parsea la respuesta del LLM al DTO de salida (ParsearRespuesta).
//
//  Esta clase base fija ese esqueleto (template method). Cada nodo concreto
//  solo implementa ConstruirPrompt y ParsearRespuesta.
//
//  NOTA DE API (MAF): el AIAgent se invoca con RunAsync(prompt,
//  cancellationToken). La respuesta se toma como texto y cada nodo concreto
//  la parsea a su DTO de salida.
// ============================================================================

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

        // 2 + 3. Llamada al LLM.
        // La API usada acepta prompt textual y CancellationToken.
        // La respuesta textual se parsea manualmente en ParsearRespuesta.
        var respuesta = await Agente.RunAsync(prompt, cancellationToken: ct);
        string textoRespuesta = respuesta.Text ?? string.Empty;

        // 4. Respuesta del LLM -> DTO de salida
        return ParsearRespuesta(message, textoRespuesta);
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
