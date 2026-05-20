using System.Text.Json.Serialization;

namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Contiene los documentos extraídos en el contexto de una auditoría.
/// Representa la entrada (input) para los agentes de análisis en la arquitectura MAF.
/// </summary>
public sealed record DocumentosExtraidos
{
    /// <summary>
    /// Identificador único de la auditoría asociada a estos documentos.
    /// </summary>
    [JsonPropertyName("auditoriaId")]
    public int AuditoriaId { get; init; }

    /// <summary>
    /// Identificador del proyecto al que pertenecen estos documentos.
    /// </summary>
    [JsonPropertyName("proyectoId")]
    public int ProyectoId { get; init; }

    /// <summary>
    /// Reglas de tailoring dinámico específicas del proyecto.
    /// </summary>
    [JsonPropertyName("reglasTailoring")]
    public List<string> ReglasTailoring { get; init; } = new List<string>();

    /// <summary>
    /// Colección de artefactos que han sido extraídos y están listos para análisis.
    /// Incluye metadatos sobre exigibilidad y estado del tailoring.
    /// </summary>
    [JsonPropertyName("artefactos")]
    public IReadOnlyList<ArtefactoExtraido> Artefactos { get; init; } = new List<ArtefactoExtraido>();
}
