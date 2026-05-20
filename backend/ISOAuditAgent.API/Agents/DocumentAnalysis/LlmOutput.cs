// ============================================================================
//  LlmOutput — DocumentAnalysis (Parte 4.C)
// ----------------------------------------------------------------------------
//  DTO interno de deserialización del JSON que devuelve el LLM. NO es parte
//  del contrato 3 — es un detalle de implementación del agente.
//
//  ORIGEN: Agent/Models/LlmOutput.cs del diff, rescate 1:1 con namespace
//  adaptado.
//
//  NOTA: el LLM responde "estadoTailoring" como string. El builder lo mapea
//  al enum EstadoAplicacionTailoring (decisión 2.8 del plan: unificación de
//  enums). Mantener el campo del JSON como string permite tolerancia
//  case-insensitive en el parseo sin acoplar el contrato del LLM a un enum.
// ============================================================================

using System.Text.Json.Serialization;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis;

internal sealed record LlmOutput(
    [property: JsonPropertyName("artefactos")]
    IReadOnlyList<LlmArtefacto> Artefactos);

internal sealed record LlmArtefacto(
    [property: JsonPropertyName("artefactoEsperadoId")]
    int ArtefactoEsperadoId,

    [property: JsonPropertyName("estadoTailoring")]
    string EstadoTailoring,

    [property: JsonPropertyName("justificacionNoAplica")]
    string? JustificacionNoAplica,

    [property: JsonPropertyName("urlReferenciaTailoring")]
    string? UrlReferenciaTailoring);
