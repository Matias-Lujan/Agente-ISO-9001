using System.Text.Json.Serialization;

namespace ISOAuditAgent.API.Agents.ComplianceValidation;

/// <summary>
/// Representa exactamente la estructura que devuelve Gemini como respuesta de análisis.
/// Este record es intermedio para deserializar correctamente el JSON del LLM.
/// </summary>
internal sealed record HallazgoLLMRespuesta
{
    /// <summary>
    /// ID del artefacto esperado que presenta el incumplimiento.
    /// Viene directamente del JSON del LLM, sin transformación.
    /// </summary>
    [JsonPropertyName("artefactoEsperadoId")]
    public int ArtefactoEsperadoId { get; init; }

    /// <summary>
    /// Descripción del incumplimiento detectado.
    /// Viene directamente del JSON del LLM, sin transformación.
    /// </summary>
    [JsonPropertyName("descripcion")]
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>
    /// Justificación técnica o normativa del incumplimiento.
    /// Viene directamente del JSON del LLM, sin transformación.
    /// </summary>
    [JsonPropertyName("justificacion")]
    public string Justificacion { get; init; } = string.Empty;

    /// <summary>
    /// Origen de la regla que se incumple.
    /// Valores esperados: "Tailoring" o "Procedimiento".
    /// Viene directamente del JSON del LLM, sin transformación.
    /// </summary>
    [JsonPropertyName("origenRegla")]
    public string OrigenRegla { get; init; } = string.Empty;
}

/// <summary>
/// Envolvente para deserializar la respuesta completa del LLM.
/// El LLM devuelve un objeto con una propiedad "hallazgos" que es un array.
/// </summary>
internal sealed record RespuestaHallazgosLLM
{
    /// <summary>
    /// Array de hallazgos devueltos por el LLM.
    /// </summary>
    [JsonPropertyName("hallazgos")]
    public List<HallazgoLLMRespuesta> Hallazgos { get; init; } = new();
}
