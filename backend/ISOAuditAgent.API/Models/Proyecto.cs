namespace ISOAuditAgent.API.Models;

public class Proyecto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public TipoProyecto TipoProyecto { get; set; }
    public int HorasEstimadas { get; set; }
    public int ProcedimientoId { get; set; }
    public string? TrelloBoardId { get; set; }
    public string? ClockifyProjectId { get; set; }
    public string? DriveFolderId { get; set; }
    public bool Activo { get; set; }

    // Navegación hacia el "uno".
    public Procedimiento Procedimiento { get; set; } = null!;

    // Navegación hacia los "muchos".
    public ICollection<ProyectoUsuario> ProyectoUsuarios { get; set; } = new List<ProyectoUsuario>();
    public ICollection<Auditoria> Auditorias { get; set; } = new List<Auditoria>();
}