namespace ISOAuditAgent.API.Models;
 
public class Hallazgo
{
    public int Id { get; set; }
    public int ArtefactoEvaluadoId { get; set; }
    public TipoHallazgo Tipo { get; set; }
    public string Descripcion { get; set; } = null!;
    public string Justificacion { get; set; } = null!;
    public AgenteOrigen AgenteOrigen { get; set; }
 
    public ArtefactoEvaluado ArtefactoEvaluado { get; set; } = null!;
}