// ============================================================================
//  ResultadoClasificacionConContexto — DTO interno del workflow (v2.2)
// ----------------------------------------------------------------------------
//  DTO wrapper que produce FindingsClassificationNode y consume
//  ConsolidadorResultadoNode. Es la ÚNICA entrada del Consolidador.
//
//  Por qué existe: el Consolidador necesita dos cosas para ensamblar el
//  AuditoriaResultado — los hallazgos clasificados y el DocumentosExtraidos
//  original (para armar la lista de artefactos evaluados y la de documentos
//  analizados). En vez de que el Consolidador reciba dos entradas por dos
//  aristas (lo que obligaría a cache mutable), FindingsClassification las
//  combina en este único wrapper y el Consolidador recibe una sola entrada.
//
//  Regla clave: el LLM de FindingsClassification NO produce ni toca el campo
//  DocumentosExtraidos. Ese campo lo conserva y lo pega el executor C#
//  (FindingsClassificationNode), por fuera del prompt. El LLM solo clasifica
//  hallazgos. DocumentosExtraidos viaja como contexto inmutable.
//
//  Carril de DocumentosExtraidos:
//    DocumentAnalysis --(edge directo)--> FindingsClassification --> Consolidador
//  Un solo carril, sin duplicación, sin pasar por los validadores.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;

namespace ISOAuditAgent.API.Agents.Contracts;

/// <summary>
/// Salida de FindingsClassificationNode. Lleva la clasificación (producida por
/// el LLM) y el DocumentosExtraidos original (conservado por el executor C#,
/// nunca tocado por el LLM).
/// </summary>
public sealed record ResultadoClasificacionConContexto(
    HallazgosClasificados Clasificacion,
    DocumentosExtraidos ContextoDocumentos
);
