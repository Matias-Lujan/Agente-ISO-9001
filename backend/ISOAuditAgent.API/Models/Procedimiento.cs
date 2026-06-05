namespace ISOAuditAgent.API.Models;
 
public class Procedimiento
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
 
    public ICollection<Etapa> Etapas { get; set; } = new List<Etapa>();
}
 