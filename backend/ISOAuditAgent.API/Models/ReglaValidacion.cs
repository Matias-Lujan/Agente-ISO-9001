namespace ISOAuditAgent.API.Models;

/// <summary>
/// Representa una regla de validación configurable obtenida de la base de datos MySQL.
/// Las reglas definen qué inconsistencias debe detectar el agente para un proceso específico.
/// </summary>
public class ReglaValidacion
{
    /// <summary>
    /// Identificador único de la regla en la base de datos.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identificador del proceso al que aplica esta regla.
    /// </summary>
    public int ProcesoId { get; set; }

    /// <summary>
    /// Descripción clara de lo que valida esta regla.
    /// Ej: "Las horas registradas en Clockify deben coincidir con las tareas en Trello".
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de regla: "Obligatorio" o "Opcional".
    /// - Obligatorio: debe validarse siempre.
    /// - Opcional: se valida solo bajo ciertas condiciones.
    /// </summary>
    public string TipoObligatorioOpcional { get; set; } = "Obligatorio";

    /// <summary>
    /// Descripción técnica o fórmula de cómo evaluar la regla.
    /// Esta información se pasará al LLM para el análisis.
    /// </summary>
    public string? CriterioEvaluacion { get; set; }

    /// <summary>
    /// Indica si la regla está activa o desactivada.
    /// </summary>
    public bool Activa { get; set; } = true;

    /// <summary>
    /// Fecha de creación de la regla.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de última actualización de la regla.
    /// </summary>
    public DateTime? FechaActualizacion { get; set; }

    /// <summary>
    /// Contexto adicional o notas sobre la regla que el LLM debe considerar.
    /// </summary>
    public string? Notas { get; set; }
}
