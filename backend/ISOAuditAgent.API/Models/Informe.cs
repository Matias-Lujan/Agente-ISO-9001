namespace ISOAuditAgent.API.Models;
 
public class Informe
{
    public int Id { get; set; }
    public int AuditoriaId { get; set; }
    public DateTime FechaGeneracion { get; set; }
    public TipoInforme Tipo { get; set; }
    public string Contenido { get; set; } = null!;
 
    public Auditoria Auditoria { get; set; } = null!;
}
 