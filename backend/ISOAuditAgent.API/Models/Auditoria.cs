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
    public bool Activo { get; set; } = true;

    // Resumen del fallo (solo cuando Estado == Fallida). Redundante con la tabla
    // registros_error_auditoria a propósito: permite mostrar el motivo en listas
    // y detalles sin hacer un join. Null mientras la auditoría no falló.
    public CategoriaErrorAuditoria? CategoriaError { get; set; }
    public string? MensajeError { get; set; }

    // Navegaci�n hacia los "uno".
    public Proyecto Proyecto { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public Etapa Etapa { get; set; } = null!;

    // Navegaci�n hacia los "muchos".
    public ICollection<ArtefactoEvaluado> ArtefactosEvaluados { get; set; } = new List<ArtefactoEvaluado>();
    public ICollection<DocumentoAnalizado> DocumentosAnalizados { get; set; } = new List<DocumentoAnalizado>();
    public ICollection<Informe> Informes { get; set; } = new List<Informe>();
    public ICollection<RegistroErrorAuditoria> RegistrosError { get; set; } = new List<RegistroErrorAuditoria>();
}