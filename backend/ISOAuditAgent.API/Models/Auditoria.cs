namespace ISOAuditAgent.API.Models;

public class Auditoria
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public int UsuarioId { get; set; }
    public int EtapaId { get; set; }
    public DateTime FechaInicioUtc { get; set; }
    public DateTime? FechaFinalizacionUtc { get; set; }
    public EstadoAuditoria Estado { get; set; }

    // Navegación hacia los "uno".
    public Proyecto Proyecto { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public Etapa Etapa { get; set; } = null!;

    // Navegación hacia los "muchos".
    public ICollection<ArtefactoEvaluado> ArtefactosEvaluados { get; set; } = new List<ArtefactoEvaluado>();
    public ICollection<DocumentoAnalizado> DocumentosAnalizados { get; set; } = new List<DocumentoAnalizado>();
    public ICollection<Informe> Informes { get; set; } = new List<Informe>();
}