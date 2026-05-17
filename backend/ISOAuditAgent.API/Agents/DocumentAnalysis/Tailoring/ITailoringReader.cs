namespace ISOAuditAgent.DocumentAnalysis.Tailoring;

/// <summary>
/// Lee el workbook de tailoring del proyecto (FR 29) desde Google Drive y
/// devuelve las filas interpretables.
/// </summary>
public interface ITailoringReader
{
    /// <summary>
    /// Localiza el XLSX de tailoring bajo la carpeta del proyecto,
    /// lo descarga y parsea las filas.
    /// </summary>
    /// <exception cref="TailoringWorkbookNotFoundException">
    /// No hay candidato XLSX reconocible.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// El archivo existe pero la tabla no tiene forma interpretable con la
    /// heurística actual (mensaje detalla el motivo).
    /// </exception>
    Task<IReadOnlyList<EntradaTailoring>> ReadAsync(
        int proyectoId,
        CancellationToken cancellationToken = default);
}
