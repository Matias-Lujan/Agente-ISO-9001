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
//   3. Si la hoja está completamente vacía (sin celdas usadas), se ignora.
//      Si tiene celdas pero no se detecta fila de encabezados (menos de 2
//      celdas no vacías en cualquier fila), se cae al fallback: una
//      SeccionDetectada con Titulo = nombre de hoja y TieneContenido = false.
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
    // Límite de filas a escanear buscando el encabezado. Evita iterar toda la
    // hoja cuando la tabla empieza tarde y los datos son extensos.
    private const int MaxFilasParaBuscarEncabezado = 20;

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
    /// En XLSX la vigencia del formulario suele estar en el HEADER/FOOTER de
    /// impresión (no en celdas), o bien en alguna celda de la cabecera. Se
    /// devuelven, por hoja: (1) el texto del header/footer de impresión y (2) el
    /// texto de las celdas usadas (normalizando fechas a dd/MM/yyyy). El escaneo
    /// del label "Vigencia" + fecha lo hace VigenciaScanner.
    /// </summary>
    public IReadOnlyList<string> ExtraerBloquesVigencia(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length == 0) return Array.Empty<string>();

        var bloques = new List<string>();
        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            using var workbook = new XLWorkbook(ms);

            foreach (var hoja in workbook.Worksheets)
            {
                // 1. Header/footer de impresión (Excel pone ahí código + vigencia).
                try
                {
                    AgregarSiNoVacio(bloques, TextoHeaderFooter(hoja.PageSetup.Header));
                    AgregarSiNoVacio(bloques, TextoHeaderFooter(hoja.PageSetup.Footer));
                }
                catch { /* PageSetup ausente o no soportado: se ignora */ }

                // 2. Celdas usadas (todas, no solo la cabecera): la vigencia puede
                //    estar en cualquier fila según el formulario.
                var rango = hoja.RangeUsed();
                if (rango is null) continue;

                var sb = new System.Text.StringBuilder();
                foreach (var celda in rango.CellsUsed())
                {
                    if (celda.DataType == XLDataType.DateTime
                        && celda.TryGetValue<DateTime>(out var dt))
                        sb.Append(dt.ToString("dd/MM/yyyy"));
                    else
                        sb.Append(celda.GetString());
                    sb.Append(' ');
                }
                AgregarSiNoVacio(bloques, sb.ToString());
            }
        }
        catch
        {
            // Robusto: devolvemos lo que se haya podido juntar.
        }

        return bloques;
    }

    private static string TextoHeaderFooter(IXLHeaderFooter? hf)
    {
        if (hf is null) return string.Empty;
        return string.Join(" ",
            hf.Left.GetText(XLHFOccurrence.AllPages),
            hf.Center.GetText(XLHFOccurrence.AllPages),
            hf.Right.GetText(XLHFOccurrence.AllPages));
    }

    private static void AgregarSiNoVacio(List<string> bloques, string texto)
    {
        if (!string.IsNullOrWhiteSpace(texto)) bloques.Add(texto);
    }

    /// <summary>
    /// Extrae SeccionDetectadas por columna de encabezado. Si no se detecta
    /// fila de encabezados, cae a sección-por-hoja (comportamiento anterior).
    /// </summary>
    private static IReadOnlyList<SeccionDetectada> ParsearHoja(IXLWorksheet hoja)
    {
        var rango = hoja.RangeUsed();
        if (rango is null)
            return [];

        int primerFila  = rango.FirstRow().RowNumber();
        int ultimaFila  = rango.LastRow().RowNumber();
        int primeraCol  = rango.FirstColumn().ColumnNumber();
        int ultimaCol   = rango.LastColumn().ColumnNumber();

        // Buscar fila de encabezados: la que tenga MÁS celdas no vacías en
        // las primeras MaxFilasParaBuscarEncabezado filas.
        // "Primera con >= 2" falla con archivos exportados desde Google Sheets:
        // las celdas fusionadas del bloque título/logo se "expanden" al exportar
        // y filas de metadata quedan con 2-4 celdas llenas, adelantándose a la
        // fila real de encabezados que tiene tantas celdas como columnas.
        int? filaEncabezado = null;
        int maxNoVacias = 1; // mínimo 2 para ser considerado encabezado
        int limiteFilaBusqueda = Math.Min(ultimaFila, primerFila + MaxFilasParaBuscarEncabezado - 1);
        for (int r = primerFila; r <= limiteFilaBusqueda; r++)
        {
            int noVacias = 0;
            for (int c = primeraCol; c <= ultimaCol; c++)
            {
                if (!string.IsNullOrWhiteSpace(hoja.Cell(r, c).GetString()))
                    noVacias++;
            }
            if (noVacias > maxNoVacias) { maxNoVacias = noVacias; filaEncabezado = r; }
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
