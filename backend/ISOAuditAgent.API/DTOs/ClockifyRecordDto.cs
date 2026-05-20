namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Representa un registro de tiempo en Clockify obtenido a través del servidor MCP.
/// </summary>
public class ClockifyRecordDto
{
    /// <summary>
    /// Identificador único del registro en Clockify.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Identificador de la tarea a la que se asocia el registro de tiempo.
    /// Puede corresponder a un ID de Trello o similar.
    /// </summary>
    public string IdTarea { get; set; } = string.Empty;

    /// <summary>
    /// Usuario que registró el tiempo.
    /// </summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>
    /// Horas registradas en esta entrada de tiempo.
    /// </summary>
    public decimal HorasRegistradas { get; set; }

    /// <summary>
    /// Fecha y hora de inicio del registro.
    /// </summary>
    public DateTime FechaInicio { get; set; }

    /// <summary>
    /// Fecha y hora de finalización del registro.
    /// </summary>
    public DateTime? FechaFin { get; set; }

    /// <summary>
    /// Descripción o comentario del trabajo realizado.
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// Identificador del proyecto en Clockify.
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Etiquetas asociadas al registro de tiempo.
    /// </summary>
    public List<string> Etiquetas { get; set; } = [];
}
