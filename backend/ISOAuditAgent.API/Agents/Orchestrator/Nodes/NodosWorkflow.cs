// ============================================================================
//  Nodos del workflow de auditoría — 6 nodos (contratos_agentes.md v2.2)
// ----------------------------------------------------------------------------
//  Grafo:
//
//    [1] ResolutorContextoNode        Executor<IniciarAuditoriaWorkflowInput,
//                                              ContextoAuditoria>      sin LLM
//    [2] DocumentAnalysisNode         Executor<ContextoAuditoria,
//                                              DocumentosExtraidos>    con LLM
//    [3] ComplianceValidationNode     Executor<DocumentosExtraidos,
//                                              HallazgosPreliminares>  con LLM
//    [4] ConsistencyVerificationNode  Executor<DocumentosExtraidos,
//                                              HallazgosPreliminares>  con LLM
//    [5] FindingsClassificationNode   Executor (3 entradas cacheadas)  con LLM
//    [6] ConsolidadorResultadoNode    Executor<ResultadoClasificacion
//                                              ConContexto,
//                                              AuditoriaResultado>     sin LLM
//
//  Aristas:
//    [1] -> [2]                                  (AddEdge)
//    [2] -> [3]  y  [2] -> [4]                   (AddFanOutEdge)
//    [2] -> [5]                                  (AddEdge: DocumentosExtraidos)
//    [3] -> [5]  y  [4] -> [5]                   (AddFanInBarrierEdge)
//    [5] -> [6]                                  (AddEdge)
//    salida desde [6]                            (WithOutputFrom)
//
//  Carril de DocumentosExtraidos: [2] -> [5] -> [6]. Un solo carril. No pasa
//  por los validadores. El LLM de [5] no lo toca: lo conserva el executor C#.
//
//  PATRONES DE EXECUTOR (los dos válidos, cada uno para su caso):
//   - Executor<TIn,TOut> + HandleAsync  -> nodo de UN input. Usan [1][2][3][4][6].
//   - Executor + [MessageHandler]       -> nodo de VARIOS inputs. Usa [5].
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace ISOAuditAgent.API.Agents.Orchestrator;

// ============================================================================
//  [1] ResolutorContextoNode — nodo determinista inicial, sin LLM
// ============================================================================

public sealed class ResolutorContextoNode
    : Executor<IniciarAuditoriaWorkflowInput, ContextoAuditoria>
{
    private readonly ResolutorContextoService _servicio;

    public ResolutorContextoNode(ResolutorContextoService servicio)
        : base("ResolutorContexto")
    {
        _servicio = servicio;
    }

    public override async ValueTask<ContextoAuditoria> HandleAsync(
        IniciarAuditoriaWorkflowInput message,
        IWorkflowContext context,
        CancellationToken ct = default)
    {
        return await _servicio.ResolverAsync(message, ct);
    }
}

// ============================================================================
//  [2] DocumentAnalysisNode — agente con LLM
// ============================================================================

public sealed class DocumentAnalysisNode
    : AgenteExecutorBase<ContextoAuditoria, DocumentosExtraidos>
{
    public DocumentAnalysisNode(AIAgent agente)
        : base("DocumentAnalysis", agente) { }

    protected override string ConstruirPrompt(ContextoAuditoria input)
    {
        throw new NotImplementedException(
            "ConstruirPrompt de DocumentAnalysis — lo implementa su dueño.");
    }

    protected override DocumentosExtraidos ParsearRespuesta(
        ContextoAuditoria input, string textoLlm)
    {
        throw new NotImplementedException(
            "ParsearRespuesta de DocumentAnalysis — lo implementa su dueño.");
    }
}

// ============================================================================
//  [3] ComplianceValidationNode — agente con LLM. Contrato 3 INTACTO.
// ============================================================================

public sealed class ComplianceValidationNode
    : AgenteExecutorBase<DocumentosExtraidos, HallazgosPreliminares>
{
    public ComplianceValidationNode(AIAgent agente)
        : base("ComplianceValidation", agente) { }

    protected override string ConstruirPrompt(DocumentosExtraidos input)
    {
        throw new NotImplementedException(
            "ConstruirPrompt de ComplianceValidation — lo implementa su dueño.");
    }

    protected override HallazgosPreliminares ParsearRespuesta(
        DocumentosExtraidos input, string textoLlm)
    {
        // AgenteOrigen = ComplianceValidation.
        throw new NotImplementedException(
            "ParsearRespuesta de ComplianceValidation — lo implementa su dueño.");
    }
}

// ============================================================================
//  [4] ConsistencyVerificationNode — agente con LLM. Contrato 3 INTACTO.
//  No valida firmas ni vigencia por fecha.
// ============================================================================

public sealed class ConsistencyVerificationNode
    : AgenteExecutorBase<DocumentosExtraidos, HallazgosPreliminares>
{
    public ConsistencyVerificationNode(AIAgent agente)
        : base("ConsistencyVerification", agente) { }

    protected override string ConstruirPrompt(DocumentosExtraidos input)
    {
        throw new NotImplementedException(
            "ConstruirPrompt de ConsistencyVerification — lo implementa su dueño.");
    }

    protected override HallazgosPreliminares ParsearRespuesta(
        DocumentosExtraidos input, string textoLlm)
    {
        // AgenteOrigen = ConsistencyVerification.
        throw new NotImplementedException(
            "ParsearRespuesta de ConsistencyVerification — lo implementa su dueño.");
    }
}

// ============================================================================
//  [5] FindingsClassificationNode — agente con LLM, TRES entradas cacheadas
// ----------------------------------------------------------------------------
//  Recibe tres mensajes, por aristas distintas y en momentos distintos:
//    - DocumentosExtraidos        edge directo desde DocumentAnalysis [2]
//    - HallazgosPreliminares (de ComplianceValidation)    fan-in desde [3]
//    - HallazgosPreliminares (de ConsistencyVerification) fan-in desde [4]
//
//  DISEÑO DE CACHEO — robusto ante la incógnita del barrier:
//  No se asume que AddFanInBarrierEdge agrupe los dos HallazgosPreliminares en
//  una lista. Se asume el caso conservador: el barrier entrega los mensajes
//  DE A UNO. El handler de HallazgosPreliminares cachea cada lote por separado,
//  distinguiéndolos por su AgenteOrigen. La clasificación se dispara recién
//  cuando están los TRES elementos cacheados (los 2 lotes + DocumentosExtraidos).
//  Si el barrier resultara agrupar en lista, basta con agregar un handler
//  IReadOnlyList<HallazgosPreliminares> — el resto del nodo no cambia.
//  // API MAF: verificar — comportamiento exacto del barrier en 1.3.0.
//
//  Distinguir los lotes por AgenteOrigen NO es "validar la estructura del
//  grafo": es mecánica necesaria del cacheo. Sin distinguirlos no se puede
//  saber si llegaron dos lotes distintos o el mismo dos veces.
//
//  Estado interno: SEGURO solo si se instancia un nodo nuevo por ejecución de
//  workflow. NO registrar como Singleton. El worker arma el workflow fresco
//  por auditoría -> garantizado.
//
//  El LLM clasifica los hallazgos. NO ve ni produce DocumentosExtraidos: lo
//  conserva este executor C# y lo pega en ResultadoClasificacionConContexto.
// ============================================================================

// API MAF: verificar — necesario para que el source generator registre el
// mensaje emitido manualmente con SendMessageAsync. Confirmar contra el paquete.
[SendsMessage(typeof(ResultadoClasificacionConContexto))]
public sealed partial class FindingsClassificationNode : Executor
{
    private readonly AIAgent _agente;

    // Los tres elementos a cachear antes de poder clasificar.
    private DocumentosExtraidos? _contextoDocumentos;
    private HallazgosPreliminares? _hallazgosCompliance;
    private HallazgosPreliminares? _hallazgosConsistency;

    public FindingsClassificationNode(AIAgent agente)
        : base("FindingsClassification")
    {
        _agente = agente;
    }

    // --- Entrada A: contexto, por edge directo desde DocumentAnalysis -------
    [MessageHandler]
    private async ValueTask HandleContextoAsync(
        DocumentosExtraidos message, IWorkflowContext context)
    {
        if (_contextoDocumentos is not null)
        {
            throw new InvalidOperationException(
                "FindingsClassification recibió DocumentosExtraidos más de " +
                "una vez. Revisar el cableado del grafo.");
        }

        _contextoDocumentos = message;
        await IntentarClasificarAsync(context);
    }

    // --- Entrada B: cada lote de hallazgos, de a uno ------------------------
    [MessageHandler]
    private async ValueTask HandleHallazgosAsync(
        HallazgosPreliminares message, IWorkflowContext context)
    {
        // Distinguir el lote por su AgenteOrigen y cachearlo en su slot.
        switch (message.AgenteOrigen)
        {
            case AgenteOrigen.ComplianceValidation:
                if (_hallazgosCompliance is not null)
                {
                    throw new InvalidOperationException(
                        "FindingsClassification recibió dos lotes de " +
                        "ComplianceValidation. Se esperaba uno solo.");
                }
                _hallazgosCompliance = message;
                break;

            case AgenteOrigen.ConsistencyVerification:
                if (_hallazgosConsistency is not null)
                {
                    throw new InvalidOperationException(
                        "FindingsClassification recibió dos lotes de " +
                        "ConsistencyVerification. Se esperaba uno solo.");
                }
                _hallazgosConsistency = message;
                break;

            default:
                throw new InvalidOperationException(
                    $"AgenteOrigen no contemplado en el fan-in: " +
                    $"{message.AgenteOrigen}.");
        }

        await IntentarClasificarAsync(context);
    }

    // --- Disparo: clasifica cuando están los TRES elementos -----------------
    private async ValueTask IntentarClasificarAsync(IWorkflowContext context)
    {
        // Falta alguno de los tres -> esperar al próximo mensaje.
        if (_contextoDocumentos is null
            || _hallazgosCompliance is null
            || _hallazgosConsistency is null)
        {
            return;
        }

        // Aserción de invariante: los tres elementos de la misma auditoría.
        int idContexto = _contextoDocumentos.AuditoriaId;
        if (_hallazgosCompliance.AuditoriaId != idContexto
            || _hallazgosConsistency.AuditoriaId != idContexto)
        {
            throw new InvalidOperationException(
                "AuditoriaId inconsistente entre los lotes de hallazgos y el " +
                "contexto recibidos por FindingsClassification.");
        }

        var lotes = new[] { _hallazgosCompliance, _hallazgosConsistency };

        // 1. Prompt de clasificación.
        string prompt = ConstruirPrompt(lotes);

        // 2 + 3. El LLM clasifica.
        // API MAF: verificar — propagar CancellationToken si la firma del
        // handler / del contexto lo expone.
        var respuesta = await _agente.RunAsync(prompt);
        string textoLlm = respuesta.Text ?? string.Empty;

        // 4. Parsear la clasificación.
        HallazgosClasificados clasificacion = ParsearRespuesta(lotes, textoLlm);

        // 5. Combinar con el contexto conservado (no pasó por el LLM).
        var salida = new ResultadoClasificacionConContexto(
            Clasificacion: clasificacion,
            ContextoDocumentos: _contextoDocumentos);

        await context.SendMessageAsync(salida, CancellationToken.None);

        // Higiene defensiva: liberar el estado cacheado.
        _contextoDocumentos = null;
        _hallazgosCompliance = null;
        _hallazgosConsistency = null;
    }

    private string ConstruirPrompt(IReadOnlyList<HallazgosPreliminares> hallazgos)
    {
        // TODO (dueño del agente): prompt con la regla de oro "si no está
        // escrito en el procedimiento -> como mucho OM".
        throw new NotImplementedException(
            "ConstruirPrompt de FindingsClassification — lo implementa su dueño.");
    }

    private HallazgosClasificados ParsearRespuesta(
        IReadOnlyList<HallazgosPreliminares> hallazgos, string textoLlm)
    {
        // TODO (dueño del agente): correspondencia 1 a 1 — no inventa, no
        // omite, no fusiona.
        throw new NotImplementedException(
            "ParsearRespuesta de FindingsClassification — lo implementa su dueño.");
    }
}

// ============================================================================
//  [6] ConsolidadorResultadoNode — nodo determinista final, sin LLM
// ----------------------------------------------------------------------------
//  Executor<TIn,TOut> simple: una sola entrada. Sin cache, sin estado mutable.
//  Ensambla el AuditoriaResultado. Lógica en ConsolidadorResultado.Ensamble.cs.
// ============================================================================

public sealed class ConsolidadorResultadoNode
    : Executor<ResultadoClasificacionConContexto, AuditoriaResultado>
{
    public ConsolidadorResultadoNode() : base("ConsolidadorResultado") { }

    public override ValueTask<AuditoriaResultado> HandleAsync(
        ResultadoClasificacionConContexto message,
        IWorkflowContext context,
        CancellationToken ct = default)
    {
        var resultado = ConsolidadorEnsamble.Ensamblar(
            message.ContextoDocumentos, message.Clasificacion);

        return ValueTask.FromResult(resultado);
    }
}
