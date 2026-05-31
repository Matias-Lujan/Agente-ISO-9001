// ============================================================================
//  Contrato 5 — Salida de FindingsClassification (contratos_agentes.md v2.2)
// ----------------------------------------------------------------------------
//  Escrito desde la especificación contratos_agentes.md v2.2 — no proviene de
//  ningún diff de los agentes.
//
//  Dos records:
//   - HallazgosClasificados: lo produce el LLM de FindingsClassification.
//     Clasifica cada hallazgo preliminar en NC/OBS/OM aplicando la regla
//     "si no está escrito en el procedimiento -> como mucho OM".
//   - ResultadoClasificacionConContexto: wrapper que combina la clasificación
//     (del LLM) con el DocumentosExtraidos original (conservado por el
//     executor C#, nunca tocado por el LLM). Es la ÚNICA entrada del nodo
//     ConsolidadorResultado, lo que lo mantiene como executor simple sin
//     estado.
//
//  Carril de DocumentosExtraidos:
//    DocumentAnalysis --(edge directo)--> FindingsClassification --> Consolidador
//  Un solo carril, sin duplicación, sin pasar por los validadores.
//
//  HallazgoClasificado se define en el contrato 6 (AuditoriaResultado.cs) y se
//  reutiliza idéntico acá.
// ============================================================================

namespace ISOAuditAgent.API.Agents.Contracts;

/// <summary>
/// Hallazgos ya clasificados en NC/OBS/OM por FindingsClassification.
/// Cada HallazgoClasificado corresponde 1 a 1 con un HallazgoPreliminar
/// recibido: FindingsClassification clasifica, no inventa, omite ni fusiona.
/// </summary>
public sealed record HallazgosClasificados(
    int AuditoriaId,
    IReadOnlyList<HallazgoClasificado> Hallazgos
);

/// <summary>
/// Salida de FindingsClassificationNode. Lleva la clasificación (producida por
/// el LLM) y el DocumentosExtraidos original (conservado por el executor C#,
/// nunca tocado por el LLM). Es la entrada única de ConsolidadorResultado.
/// </summary>
public sealed record ResultadoClasificacionConContexto(
    HallazgosClasificados Clasificacion,
    DocumentosExtraidos ContextoDocumentos
);