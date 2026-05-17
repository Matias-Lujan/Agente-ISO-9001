namespace ISOAuditAgent.API.Models;

/// <summary>
/// Representa un proyecto de la empresa.
/// Mapea exactamente a la tabla "proyecto" en Supabase.
/// </summary>
public class Proyecto
{
    public int Id { get; set; }

    // Nombre del proyecto
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public DateTime? FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    // Tipo A = mas de 1200 horas, Tipo B = hasta 1200 horas
    // Determina que artefactos son obligatorios
    public string TipoProyecto { get; set; } = "B";

    public int? HorasEstimadas { get; set; }

    // Procedimiento que rige el proyecto (PR 11-13 en el caso de BDT)
    public int ProcedimientoId { get; set; }

    // IDs de integraciones externas — son opcionales
    // porque no todos los proyectos usan todas las herramientas
    public string? TrelloBoardId { get; set; }
    public string? ClockifyProjectId { get; set; }
    public string? DriveFolderId { get; set; }

    // Permite deshabilitar un proyecto sin borrarlo
    // para mantener el historial de auditorias
    public bool Activo { get; set; } = true;
}

/// <summary>
/// Representa la asignacion de un usuario a un proyecto.
/// Mapea a la tabla "proyecto_usuario" en Supabase.
/// Un proyecto puede tener varios usuarios asignados.
/// Un usuario puede estar en varios proyectos.
/// </summary>
public class ProyectoUsuario
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public int UsuarioId { get; set; }
}

/// <summary>
/// Tipos de proyecto segun horas estimadas.
/// </summary>
public static class TiposProyecto
{
    // Mas de 1200 horas — todos los artefactos son obligatorios
    public const string A = "A";
    // Hasta 1200 horas — algunos artefactos pueden no aplicar con justificacion
    public const string B = "B";
}