namespace ISOAuditAgent.API.Models;
 
public class ArtefactoEsperado
{
    public int Id { get; set; }
    public int EtapaId { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool MandatorioTipoA { get; set; }
    public bool MandatorioTipoB { get; set; }
    public string? PathTemplateRelativo { get; set; }
 
    public Etapa Etapa { get; set; } = null!;
    public ICollection<ArtefactoEvaluado> ArtefactosEvaluados { get; set; } = new List<ArtefactoEvaluado>();
}
 