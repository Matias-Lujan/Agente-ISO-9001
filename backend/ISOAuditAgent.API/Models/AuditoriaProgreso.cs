namespace ISOAuditAgent.API.Models;

public class AuditoriaProgreso
{
    public int Id { get; set; }
    public int AuditoriaId { get; set; }
    public NodoWorkflow Nodo { get; set; }
    public EstadoProgreso Estado { get; set; }
    public DateTime? FechaInicioUtc { get; set; }
    public DateTime? FechaFinUtc { get; set; }

    public Auditoria Auditoria { get; set; } = null!;
}
