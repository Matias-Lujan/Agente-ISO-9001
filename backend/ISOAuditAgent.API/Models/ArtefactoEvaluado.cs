namespace ISOAuditAgent.API.Models;

public class ArtefactoEvaluado
{
    public int Id { get; set; }
    public int AuditoriaId { get; set; }
    public int ArtefactoEsperadoId { get; set; }
    public EstadoAplicacionTailoring Aplica { get; set; }
    public string? JustificacionNoAplica { get; set; }
    public ResultadoEvaluacion Resultado { get; set; }
    public string? Observaciones { get; set; }

    // Navegación hacia los "uno".
    public Auditoria Auditoria { get; set; } = null!;
    public ArtefactoEsperado ArtefactoEsperado { get; set; } = null!;

    // Navegación hacia los "muchos".
    public ICollection<Hallazgo> Hallazgos { get; set; } = new List<Hallazgo>();
    public ICollection<DocumentoAnalizado> DocumentosAnalizados { get; set; } = new List<DocumentoAnalizado>();
}