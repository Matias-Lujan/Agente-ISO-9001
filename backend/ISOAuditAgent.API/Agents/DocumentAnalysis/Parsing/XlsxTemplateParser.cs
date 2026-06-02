// ============================================================================
//  XlsxTemplateParser — Parser de .xlsx con ClosedXML (D3.3)
// ----------------------------------------------------------------------------
//  Estrategia: una SeccionDetectada por columna de la fila de encabezados.
//
//  Para cada hoja del workbook:
//   1. Se busca la primera fila con al menos 2 celdas no vacías → fila de
//      encabezados (típicamente fila 1, pero tolerante a hojas con metadata
//      en las primeras filas).
//   2. Por cada celda no vacía de esa fila se emite una SeccionDetectada:
//        Titulo         = texto de la celda de encabezado (ej. "Riesgo").
//        TieneContenido = hay al menos una celda no vacía en esa columna
//                         debajo de la fila de encabezados.
//   3. Si la hoja está vacía o no tiene fila de encabezados detectable
//      (menos de 2 celdas no vacías en cualquier fila), se cae al fallback:
//      una SeccionDetectada con Titulo = nombre de hoja y TieneContenido =
//      hay alguna celda no vacía en la hoja.
//
//  CONSECUENCIA PARA LA COMPARACIÓN TEMPLATE vs DOCUMENTO:
//  El template define qué columnas se esperan (sus SeccionDetectadas indican
//  los encabezados). El documento produce las mismas SeccionDetectadas con
//  TieneContenido = tiene datos reales en esa columna. HallazgosEstructurales
//  compara por NormalizeHeaderKey y detecta columnas ausentes o vacías.
//
//  NOTA — FR-29 NO usa este parser. El FR-29 (tailoring) tiene parser
//  dedicado en TailoringReader, porque su estructura no es "hoja = sección"
//  sino "hoja principal con columnas específicas".
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

            var resultado = new List<SeccionDetectada>(workbook.Worksheets.Count * 8);

            foreach (var hoja in workbook.Worksheets)
            {
                resultado.AddRange(ParsearHoja(hoja));
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
    /// Extrae SeccionDetectadas por columna de encabezado. Si no se detecta
    /// fila de encabezados, cae a sección-por-hoja (comportamiento anterior).
    /// </summary>
    private static IReadOnlyList<SeccionDetectada> ParsearHoja(IXLWorksheet hoja)
    {
        var rango = hoja.RangeUsed();
        if (rango is null)
            return [new SeccionDetectada(hoja.Name, false)];

        int primerFila  = rango.FirstRow().RowNumber();
        int ultimaFila  = rango.LastRow().RowNumber();
        int primeraCol  = rango.FirstColumn().ColumnNumber();
        int ultimaCol   = rango.LastColumn().ColumnNumber();

        // Buscar fila de encabezados: primera fila con >= 2 celdas no vacías.
        int? filaEncabezado = null;
        for (int r = primerFila; r <= ultimaFila; r++)
        {
            int noVacias = 0;
            for (int c = primeraCol; c <= ultimaCol; c++)
            {
                if (!string.IsNullOrWhiteSpace(hoja.Cell(r, c).GetString()))
                    noVacias++;
            }
            if (noVacias >= 2) { filaEncabezado = r; break; }
        }

        // Sin fila de encabezados detectable → fallback de sección-por-hoja.
        if (filaEncabezado is null)
        {
            return [new SeccionDetectada(hoja.Name, false)];
        }

        // Una SeccionDetectada por columna de encabezado.
        var secciones = new List<SeccionDetectada>(ultimaCol - primeraCol + 1);

        for (int c = primeraCol; c <= ultimaCol; c++)
        {
            var encabezado = hoja.Cell(filaEncabezado.Value, c).GetString().Trim();
            if (string.IsNullOrWhiteSpace(encabezado)) continue;

            // TieneContenido = al menos una celda no vacía debajo del encabezado.
            bool tieneContenido = false;
            for (int r = filaEncabezado.Value + 1; r <= ultimaFila; r++)
            {
                if (!string.IsNullOrWhiteSpace(hoja.Cell(r, c).GetString()))
                {
                    tieneContenido = true;
                    break;
                }
            }

            secciones.Add(new SeccionDetectada(encabezado, tieneContenido));
        }

        // Si por algún motivo no se pudo extraer ninguna columna, fallback.
        return secciones.Count > 0
            ? secciones
            : [new SeccionDetectada(hoja.Name, HojaTieneContenido(hoja, filaEncabezado.Value))];
    }

    private static bool HojaTieneContenido(IXLWorksheet hoja, int filaEncabezado)
    {
        var rango = hoja.RangeUsed();
        if (rango is null) return false;
        return rango.CellsUsed()
            .Any(c => c.Address.RowNumber > filaEncabezado
                   && !string.IsNullOrWhiteSpace(c.GetString()));
    }
}
