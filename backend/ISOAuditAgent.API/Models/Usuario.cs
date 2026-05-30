namespace ISOAuditAgent.API.Models;
 
public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public RolUsuario Rol { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
 
    public ICollection<ProyectoUsuario> ProyectoUsuarios { get; set; } = new List<ProyectoUsuario>();
    public ICollection<Auditoria> Auditorias { get; set; } = new List<Auditoria>();
}