namespace ISOAuditAgent.API.Models;

/// <summary>
/// Representa un hallazgo de inconsistencia detectado por el Agente de Validación de Procesos.
/// Contiene la descripción del desvío sin incluir clasificación (eso es responsabilidad del Agente de Clasificación).
/// </summary>
public class ValidationFinding
{
    /// <summary>
    /// Identificador único del hallazgo (generado por el agente).
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Descripción detallada del desvío o inconsistencia detectada.
    /// Ej: "La tarea TRE-123 tiene fecha de vencimiento 2024-01-15, pero el último registro en Clockify es del 2024-01-18".
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Origen del hallazgo: qué fuentes de datos se compararon.
    /// Ej: "Trello vs Clockify", "Datos internos vs Trello".
    /// </summary>
    public string Fuente { get; set; } = string.Empty;

    /// <summary>
    /// Identificador de la tarea relacionada (si aplica).
    /// </summary>
    public string? IdTareaRelacionada { get; set; }

    /// <summary>
    /// Regla de validación que se incumplió.
    /// </summary>
    public string? ReglaIncumplida { get; set; }

    /// <summary>
    /// Detalles técnicos adicionales del hallazgo (JSON con información específica).
    /// </summary>
    public string? DetallesJSON { get; set; }

    /// <summary>
    /// Timestamp de cuándo se detectó el hallazgo.
    /// </summary>
    public DateTime FechaDeteccion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identificador del proceso al que pertenece este hallazgo.
    /// </summary>
    public int ProcesoId { get; set; }

    /// <summary>
    /// Identificador del proyecto analizado.
    /// </summary>
    public string? ProjectId { get; set; }
}
