namespace ISOAuditAgent.API.Models;
 
public class ProyectoUsuario
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public int UsuarioId { get; set; }
 
    public Proyecto Proyecto { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}
 