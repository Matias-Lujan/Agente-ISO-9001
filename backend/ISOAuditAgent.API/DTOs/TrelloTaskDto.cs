namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Representa una tarea de Trello obtenida a través del servidor MCP.
/// </summary>
public class TrelloTaskDto
{
    /// <summary>
    /// Identificador único de la tarea en Trello.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nombre o título de la tarea.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Usuario responsable de la tarea.
    /// </summary>
    public string Responsable { get; set; } = string.Empty;

    /// <summary>
    /// Estado actual de la tarea (ej: "En Progreso", "Completado").
    /// </summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de inicio planificada.
    /// </summary>
    public DateTime? FechaInicio { get; set; }

    /// <summary>
    /// Fecha de vencimiento o finalización planificada.
    /// </summary>
    public DateTime? FechaVencimiento { get; set; }

    /// <summary>
    /// Fecha real de finalización (si aplica).
    /// </summary>
    public DateTime? FechaFinalizacion { get; set; }

    /// <summary>
    /// Identificador del proyecto al que pertenece la tarea.
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Etiquetas o labels asignadas a la tarea.
    /// </summary>
    public List<string> Etiquetas { get; set; } = [];
}
