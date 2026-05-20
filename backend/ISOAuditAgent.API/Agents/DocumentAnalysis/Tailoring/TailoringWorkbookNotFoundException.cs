namespace ISOAuditAgent.DocumentAnalysis.Tailoring;

/// <summary>
/// Falla de configuración: no hay ningún archivo reconocible de tailoring
/// (FR 29) bajo la carpeta Drive del proyecto.
/// </summary>
public sealed class TailoringWorkbookNotFoundException : InvalidOperationException
{
    public TailoringWorkbookNotFoundException(int proyectoId)
        : base(
            $"No se encontró un archivo XLSX de tailoring (FR 29) en la carpeta " +
            $"Drive del proyecto {proyectoId}. Verifique que exista un libro " +
            $"cuyo nombre sugiera FR 29 o \"Tailoring\".")
    {
        ProyectoId = proyectoId;
    }

    public int ProyectoId { get; }
}
