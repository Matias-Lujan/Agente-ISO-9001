using System.Text.Json.Serialization;

namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Representa un artefacto (documento o evidencia) que ha sido extraído durante una auditoría ISO 9001.
/// Contiene metadatos sobre la exigibilidad y estado del tailoring del artefacto.
/// </summary>
public sealed record ArtefactoExtraido
{
    /// <summary>
    /// Identificador único del artefacto esperado en el estándar ISO 9001.
    /// Referencia al catálogo de artefactos requeridos.
    /// </summary>
    [JsonPropertyName("artefactoEsperadoId")]
    public int ArtefactoEsperadoId { get; init; }

    /// <summary>
    /// Nombre descriptivo del artefacto (ej: "Manual de Calidad", "Procedimiento de RH").
    /// </summary>
    [JsonPropertyName("nombreArtefacto")]
    public string NombreArtefacto { get; init; } = string.Empty;

    /// <summary>
    /// Define si el artefacto es exigible en la etapa actual de auditoría.
    /// Puede ser Exigible o PendienteEtapaFutura.
    /// </summary>
    [JsonPropertyName("exigibilidad")]
    public ExigibilidadArtefacto Exigibilidad { get; init; }

    /// <summary>
    /// Estado de aplicabilidad según el tailoring dinámico del proyecto.
    /// Indica si el artefacto aplica o no aplica según reglas específicas.
    /// </summary>
    [JsonPropertyName("estadoTailoring")]
    public EstadoTailoring EstadoTailoring { get; init; }

    /// <summary>
    /// Justificación de por qué el artefacto no aplica al proyecto.
    /// Solo se completa cuando EstadoTailoring = NoAplica.
    /// </summary>
    [JsonPropertyName("justificacionNoAplica")]
    public string JustificacionNoAplica { get; init; } = string.Empty;

    /// <summary>
    /// Contenido o resumen del documento que representa al artefacto.
    /// Puede ser texto extractado o descripción del contenido.
    /// </summary>
    [JsonPropertyName("contenido")]
    public string Contenido { get; init; } = string.Empty;

    /// <summary>
    /// Tipo de documento (ej: "PDF", "Excel", "Word", "Documento Digital").
    /// </summary>
    [JsonPropertyName("tipo")]
    public string Tipo { get; init; } = string.Empty;

    /// <summary>
    /// Identificador único del artefacto extraído en el sistema.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
}
