namespace ISOAuditAgent.API.Models;

/// <summary>
/// Representa una auditoria ejecutada sobre un proyecto.
/// Mapea exactamente a la tabla "auditoria" en Supabase.
/// </summary>
public class Auditoria
{
    public int Id { get; set; }

    // Proyecto que se esta auditando
    public int ProyectoId { get; set; }

    // Usuario auditor que ejecuta la auditoria
    public int UsuarioId { get; set; }

    // Etapa del proyecto que se esta auditando
    public int EtapaId { get; set; }

    // Fecha y hora en que se inicio la auditoria (en UTC)
    public DateTime FechaInicioUtc { get; set; } = DateTime.UtcNow;

    // Fecha y hora en que termino — null si esta en curso
    public DateTime? FechaFinalizacionUtc { get; set; }

    // Horas estimadas del proyecto al momento de la auditoria
    public int? HorasEstimadas { get; set; }

    // Estado actual de la auditoria
    public string Estado { get; set; } = EstadosAuditoria.EnCurso;

    // Permite deshabilitar auditorias sin borrarlas
    public bool Activo { get; set; } = true;
}

/// <summary>
/// Estados posibles de una auditoria.
/// </summary>
public static class EstadosAuditoria
{
    // El workflow de agentes esta corriendo
    public const string EnCurso    = "en_curso";
    // El workflow termino correctamente
    public const string Completada = "completada";
    // El workflow termino con un error
    public const string Fallida    = "fallida";
}