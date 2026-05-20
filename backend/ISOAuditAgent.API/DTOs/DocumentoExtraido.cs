using System.Text.Json.Serialization;

namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Representa un documento que ha sido extraído o procesado durante una auditoría.
/// Contiene información esencial del documento para el análisis de cumplimiento.
/// </summary>
public sealed record DocumentoExtraido
{
    /// <summary>
    /// Identificador único del documento.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Nombre o título del documento.
    /// </summary>
    [JsonPropertyName("nombre")]
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Tipo de documento (ej: "PDF", "Excel", "Word", "Imagen").
    /// </summary>
    [JsonPropertyName("tipo")]
    public string Tipo { get; init; } = string.Empty;

    /// <summary>
    /// Contenido o resumen del documento. Puede ser texto extracto o descripción.
    /// </summary>
    [JsonPropertyName("contenido")]
    public string Contenido { get; init; } = string.Empty;

    /// <summary>
    /// Fecha en que el documento fue creado o modificado.
    /// </summary>
    [JsonPropertyName("fechaCreacion")]
    public DateTime FechaCreacion { get; init; }

    /// <summary>
    /// Autor o responsable del documento.
    /// </summary>
    [JsonPropertyName("autor")]
    public string Autor { get; init; } = string.Empty;
}
