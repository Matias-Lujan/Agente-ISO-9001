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

    // Origen donde se verifica el artefacto (Drive / Trello / Clockify) — viene de dev (ML-06).
    public FuenteVerificacion FuenteVerificacion { get; set; } = FuenteVerificacion.Drive;

    // Marca el artefacto que ES el tailoring del procedimiento (FR 29 en PR 11-13).
    // TailoringReader lo usa para saber qué archivo buscar en el Drive del proyecto,
    // por su código/nombre, en vez de hardcodear "FR 29". Debe haber exactamente uno
    // por procedimiento.
    public bool EsTailoring { get; set; }

    public Etapa Etapa { get; set; } = null!;
    public ICollection<ArtefactoEvaluado> ArtefactosEvaluados { get; set; } = new List<ArtefactoEvaluado>();
}
