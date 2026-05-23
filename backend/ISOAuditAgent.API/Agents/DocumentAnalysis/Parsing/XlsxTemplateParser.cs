// ============================================================================
//  XlsxTemplateParser — Parser de .xlsx con ClosedXML (D3.3)
// ----------------------------------------------------------------------------
//  Estrategia simplificadora: cada HOJA del workbook es una sección.
//   - Titulo  = nombre de la hoja.
//   - TieneContenido = la hoja tiene al menos una celda no vacía debajo
//     de la fila 1 (asumimos que la fila 1 puede ser solo encabezados).
//
//  CASO NO CUBIERTO: detectar headings dentro de una misma hoja por estilo
//  de celda. Es mucho más código y propenso a falsos positivos; queda
//  pendiente si en D3.4/D3.5 aparece un caso real que lo justifique.
//
//  NOTA — FR-29 NO usa este parser. El FR-29 (tailoring) tiene parser
//  dedicado en D3.4 (TailoringReader), porque su estructura no es "hoja
//  = sección" sino "hoja principal con columnas específicas".
// ============================================================================

using ClosedXML.Excel;
using ISOAuditAgent.API.Agents.Contracts;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;

public sealed class XlsxTemplateParser : ITemplateParser
{
    public IReadOnlyList<SeccionDetectada> Parsear(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException(
                "XlsxTemplateParser: el archivo está vacío (0 bytes).");
        }

        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            using var workbook = new XLWorkbook(ms);

            var resultado = new List<SeccionDetectada>(workbook.Worksheets.Count);

            foreach (var hoja in workbook.Worksheets)
            {
                resultado.Add(new SeccionDetectada(
                    Titulo: hoja.Name,
                    TieneContenido: HojaTieneContenido(hoja)));
            }

            return resultado;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error parseando XLSX: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Devuelve true si hay al menos una celda no vacía debajo de la fila 1.
    /// La fila 1 se asume "encabezados" — una hoja con solo encabezados se
    /// considera sin contenido real.
    /// </summary>
    private static bool HojaTieneContenido(IXLWorksheet hoja)
    {
        // RangeUsed devuelve solo el rango con datos; si la hoja está vacía,
        // es null.
        var rango = hoja.RangeUsed();
        if (rango is null) return false;

        // Si el rango usado solo cubre la fila 1, no hay contenido.
        if (rango.LastRow().RowNumber() < 2) return false;

        // Hay al menos una celda no vacía en fila 2 o posterior.
        return rango.CellsUsed()
            .Any(c => c.Address.RowNumber >= 2 && !string.IsNullOrWhiteSpace(c.GetString()));
    }
}