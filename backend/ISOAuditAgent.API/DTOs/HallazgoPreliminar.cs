using System.Text.Json.Serialization;

namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Representa un hallazgo preliminar detectado durante el análisis de auditoría ISO 9001.
/// Describe un desvío, omisión o anomalía en relación a un artefacto específico.
/// </summary>
public sealed record HallazgoPreliminar
{
    /// <summary>
    /// Identificador del artefacto esperado relacionado con este hallazgo.
    /// Establece la conexión entre el hallazgo y el artefacto que debería estar presente o en cumplimiento.
    /// </summary>
    [JsonPropertyName("artefactoEsperadoId")]
    public int ArtefactoEsperadoId { get; init; }

    /// <summary>
    /// Descripción clara y concisa del desvío, omisión o anomalía encontrada.
    /// Debe ser objetiva y basada en hechos concretos identificados durante el análisis.
    /// </summary>
    [JsonPropertyName("descripcion")]
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>
    /// Justificación técnica o normativa de por qué esto constituye un hallazgo.
    /// Puede referenciar requisitos ISO 9001, criterios de evaluación, reglas de tailoring, etc.
    /// </summary>
    [JsonPropertyName("justificacion")]
    public string Justificacion { get; init; } = string.Empty;

    /// <summary>
    /// Indica el origen o fuente de la regla que se incumple.
    /// Valores típicos: "Tailoring" (regla específica del proyecto) o "Procedimiento" (norma ISO 9001 estándar).
    /// </summary>
    [JsonPropertyName("origenRegla")]
    public string OrigenRegla { get; init; } = string.Empty;
}
