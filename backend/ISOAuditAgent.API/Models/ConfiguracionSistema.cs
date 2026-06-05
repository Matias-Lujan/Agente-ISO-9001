namespace ISOAuditAgent.API.Models;
 
public class ConfiguracionSistema
{
    public int Id { get; set; }
    public string Clave { get; set; } = null!;
    public string Valor { get; set; } = null!;
    public string? Descripcion { get; set; }
}
 