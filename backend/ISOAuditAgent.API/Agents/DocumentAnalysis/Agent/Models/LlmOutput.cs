using System.Text.Json.Serialization;

namespace ISOAuditAgent.DocumentAnalysis.Agent.Models;

public sealed record LlmOutput(
    [property: JsonPropertyName("artefactos")] IReadOnlyList<LlmArtefacto> Artefactos);

public sealed record LlmArtefacto(
    [property: JsonPropertyName("artefactoEsperadoId")] int ArtefactoEsperadoId,
    [property: JsonPropertyName("estadoTailoring")] string EstadoTailoring,
    [property: JsonPropertyName("justificacionNoAplica")]
    string? JustificacionNoAplica,
    [property: JsonPropertyName("urlReferenciaTailoring")]
    string? UrlReferenciaTailoring);
