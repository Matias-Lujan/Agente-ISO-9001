using System.Text.Json.Serialization;

namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Representa una pieza de evidencia que respalda un hallazgo preliminar.
/// Utilizado en la arquitectura MAF para documentar las bases de cada hallazgo.
/// </summary>
public sealed record EvidenciaHallazgo
{
    /// <summary>
    /// Tipo de evidencia (ej: "Registro Clockify", "Tarea Trello", "Documento").
    /// </summary>
    [JsonPropertyName("tipo")]
    public string Tipo { get; init; } = string.Empty;

    /// <summary>
    /// Identificador único de la fuente de evidencia.
    /// </summary>
    [JsonPropertyName("idFuente")]
    public string IdFuente { get; init; } = string.Empty;

    /// <summary>
    /// Descripción detallada de la evidencia.
    /// </summary>
    [JsonPropertyName("descripcion")]
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>
    /// Datos técnicos asociados a la evidencia en formato JSON serializado.
    /// </summary>
    [JsonPropertyName("datosJSON")]
    public string DatosJSON { get; init; } = string.Empty;
}
