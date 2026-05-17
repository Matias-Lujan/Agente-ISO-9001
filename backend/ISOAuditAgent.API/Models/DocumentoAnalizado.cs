namespace ISOAuditAgent.API.Models;

public class DocumentoAnalizado
{
    public int Id { get; set; }
    public int AuditoriaId { get; set; }
    public int ArtefactoEvaluadoId { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public FuenteDocumento Fuente { get; set; }
    public string? UrlReferencia { get; set; }
    public string HashContenido { get; set; } = null!;

    // Navegación hacia los "uno".
    public Auditoria Auditoria { get; set; } = null!;
    public ArtefactoEvaluado ArtefactoEvaluado { get; set; } = null!;
}