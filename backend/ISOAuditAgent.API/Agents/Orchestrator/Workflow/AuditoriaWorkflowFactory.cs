// ============================================================================
//  AuditoriaWorkflowFactory — Armado del grafo del workflow de auditoría
// ----------------------------------------------------------------------------
//  Construye el workflow MAF de 6 nodos. Es una FACTORY, no un singleton:
//  produce un workflow NUEVO en cada llamada a Crear().
//
//  Por qué factory y workflow fresco por auditoría:
//  FindingsClassificationNode cachea estado (los 3 elementos a clasificar).
//  Ese cache es seguro SOLO si cada auditoría usa una instancia nueva del
//  nodo. El background worker llama a Crear() una vez por auditoría, con
//  nodos frescos.
//
//  Grafo (contratos_agentes.md v2.2):
//
//    [1] ResolutorContexto ──► [2] DocumentAnalysis ──┬──► [3] Compliance ──┐
//                                     │               │                    │
//                                     │               └──► [4] Consistency ┤
//                                     │                                    │
//                                     │  (edge directo: DocumentosExtraidos)│
//                                     └──────────────────────┐             │
//                                                            ▼             ▼
//                                              [5] FindingsClassification ◄─┘
//                                                            │   (fan-in barrier
//                                                            │    desde [3] y [4])
//                                                            ▼
//                                              [6] ConsolidadorResultado ──► OUTPUT
//
//  NOTA SOBRE BindExecutor():
//  La API de MAF 1.3.0 requiere ExecutorBinding (no Executor) en los métodos
//  del WorkflowBuilder. Cada nodo se bindea UNA sola vez, en una variable
//  local, y esa variable se reutiliza en todas las aristas donde participa.
//  Esto garantiza que el builder vea el MISMO binding para un nodo en todas
//  sus aristas, sin depender de cómo el builder resuelve la identidad de los
//  nodos (por ID string o por identidad de objeto).
//
//  API MAF: verificar — la firma de AddFanInBarrierEdge se usa con argumentos
//  posicionales (sources, target) por una ambigüedad de overloads con named
//  arguments. Confirmar en el IntelliSense que el overload elegido es el de
//  fan-in barrier (varias fuentes -> un destino con sincronización), no otro.
// ============================================================================

using Microsoft.Agents.AI.Workflows;

namespace ISOAuditAgent.API.Agents.Orchestrator;

/// <summary>
/// Conjunto de los 6 nodos de UNA auditoría, ya construidos. Lo arma el
/// background worker (resolviendo cada nodo del scope de DI de esa auditoría).
/// </summary>
public sealed record NodosAuditoria(
    ResolutorContextoNode Resolutor,
    DocumentAnalysisNode DocumentAnalysis,
    ComplianceValidationNode ComplianceValidation,
    ConsistencyVerificationNode ConsistencyVerification,
    FindingsClassificationNode FindingsClassification,
    ConsolidadorResultadoNode Consolidador
);

/// <summary>
/// Factory del workflow de auditoría. Sin estado: solo cablea el grafo.
/// </summary>
public static class AuditoriaWorkflowFactory
{
    /// <summary>
    /// Construye un workflow nuevo a partir de los nodos de una auditoría.
    /// Nodo de entrada: ResolutorContexto [1]. Nodo de salida:
    /// ConsolidadorResultado [6].
    /// </summary>
    public static Workflow Crear(NodosAuditoria nodos)
    {
        // Cada nodo se bindea UNA sola vez. Las variables se reutilizan en
        // todas las aristas: garantiza un binding único por nodo.
        var resolutor       = nodos.Resolutor.BindExecutor();
        var documentAnalysis = nodos.DocumentAnalysis.BindExecutor();
        var compliance      = nodos.ComplianceValidation.BindExecutor();
        var consistency     = nodos.ConsistencyVerification.BindExecutor();
        var findings        = nodos.FindingsClassification.BindExecutor();
        var consolidador    = nodos.Consolidador.BindExecutor();

        // El WorkflowBuilder se inicializa con el nodo de entrada del grafo.
        var builder = new WorkflowBuilder(resolutor);

        // --- [1] -> [2] : ResolutorContexto entrega ContextoAuditoria -------
        builder.AddEdge(resolutor, documentAnalysis);

        // --- [2] -> [3] y [2] -> [4] : fan-out a los dos validadores --------
        // DocumentAnalysis emite DocumentosExtraidos; ambos validadores lo
        // reciben en paralelo y aplican criterios distintos.
        builder.AddFanOutEdge(
            documentAnalysis,
            targets: new[] { compliance, consistency });

        // --- [2] -> [5] : edge directo del contexto -------------------------
        // DocumentosExtraidos viaja también, por un carril propio, hasta
        // FindingsClassification, que lo conserva para el Consolidador.
        // No pasa por los validadores: un solo carril, sin duplicación.
        builder.AddEdge(documentAnalysis, findings);

        // --- [3] -> [5] y [4] -> [5] : fan-in barrier -----------------------
        // FindingsClassification no arranca la clasificación hasta tener los
        // dos lotes de HallazgosPreliminares. El barrier sincroniza: espera a
        // que ambos validadores terminen.
        // Argumentos posicionales (sources, target) — ver nota de cabecera.
        builder.AddFanInBarrierEdge(
            new[] { compliance, consistency },
            findings);

        // --- [5] -> [6] : la clasificación + contexto van al Consolidador ---
        builder.AddEdge(findings, consolidador);

        // --- Salida del workflow : el AuditoriaResultado del Consolidador ---
        builder.WithOutputFrom(consolidador);

        return builder.Build();
    }
}
