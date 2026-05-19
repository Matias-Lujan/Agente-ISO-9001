using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Extrae la primera hoja de un <c>.xlsx</c> como filas de celdas de texto,
/// para interpretación estructurada (p. ej. tailoring FR 29) sin pasar por
/// Markdown.
/// </summary>
/// <remarks>
/// <para>
/// Tolerancia a planillas irregulares (Fase C del plan): se elige como fila
/// de encabezados aquella entre las primeras 15 filas con datos que maximice
/// una puntuación heurística frente a palabras clave típicas de tailoring
/// (<i>codigo</i>, <i>etapa</i>, <i>aplica</i>, <i>url</i>, etc.). Si la
/// primera fila es un título, suele quedar descartada automáticamente.
/// </para>
/// <para>
/// Si la variabilidad real del cliente supera esta heurística, el plan
/// prevé delegar la interpretación a una tool del agente LLM (Fase F).
/// </para>
/// </remarks>
public sealed class XlsxTableExtractor
{
    private static readonly string[] HeaderHintFragments =
    {
        "codigo", "código", "cod", "etapa", "fase", "aplica", "url", "link",
        "ubic", "justif", "referencia", "fr-", "fr "
    };

    /// <summary>
    /// Lee la hoja indicada (o la primera con filas) y devuelve encabezados +
    /// filas de datos como listas de valores por columnas (relleno vacío a
    /// la derecha según el ancho máximo observado).
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> ExtractRows(
        Stream contenido,
        string nombreArchivo,
        string? preferredSheetName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contenido);
        cancellationToken.ThrowIfCancellationRequested();

        Stream? seekable = null;
        try
        {
            seekable = EnsureSeekable(contenido);

            OpenXmlPackageDiagnostics.ValidateLooksLikeOpenXml(seekable, nombreArchivo);

            using var doc = SpreadsheetDocument.Open(seekable, isEditable: false);

            var workbookPart = doc.WorkbookPart
                ?? throw new InvalidOperationException(
                    "El paquete OpenXml es válido pero no contiene WorkbookPart.");

            var sharedStrings = SpreadsheetCellValueResolver.LoadSharedStrings(workbookPart);
            var sheets = workbookPart.Workbook?.Sheets?.OfType<Sheet>().ToList()
                         ?? new List<Sheet>();

            var sheet = SelectSheet(workbookPart, sheets, preferredSheetName);
            if (sheet?.Id?.Value is not string relId)
            {
                return Array.Empty<IReadOnlyList<string>>();
            }

            var part = (WorksheetPart)workbookPart.GetPartById(relId);
            if (part.Worksheet is null) return Array.Empty<IReadOnlyList<string>>();

            var rows = part.Worksheet.Descendants<Row>().ToList();
            if (rows.Count == 0) return Array.Empty<IReadOnlyList<string>>();

            var matrix = new List<List<string>>(rows.Count);
            var maxCols = 0;

            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cells = new List<string>();
                foreach (var cell in row.Elements<Cell>())
                {
                    cells.Add(SpreadsheetCellValueResolver.ResolveCellValue(cell, sharedStrings));
                }

                if (cells.Count > maxCols) maxCols = cells.Count;
                matrix.Add(cells);
            }

            if (maxCols == 0) return Array.Empty<IReadOnlyList<string>>();

            foreach (var row in matrix)
            {
                while (row.Count < maxCols) row.Add(string.Empty);
            }

            return matrix;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var details = OpenXmlPackageDiagnostics.DescribeFailure(
                ex, seekable, nombreArchivo,
                "XLSX tabla (OpenXml SpreadsheetML)");

            throw new InvalidOperationException(
                $"Error extrayendo tabla XLSX: {details}", ex);
        }
    }

    /// <summary>
    /// Igual que <see cref="ExtractRows"/> pero omite filas en blanco al
    /// final y localiza automáticamente la fila de encabezados.
    /// </summary>
    public (int HeaderRowIndex, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyDictionary<string, string>> DataRows)
        ExtractKeyedTable(
        Stream contenido,
        string nombreArchivo,
        string? preferredSheetName = null,
        CancellationToken cancellationToken = default)
    {
        var matrix = ExtractRows(contenido, nombreArchivo, preferredSheetName, cancellationToken)
            .Select(static r => r.ToList())
            .ToList();

        TrimTrailingEmptyRows(matrix);

        if (matrix.Count == 0)
        {
            throw new InvalidOperationException(
                $"La planilla '{nombreArchivo}' no contiene filas de datos.");
        }

        var headerRowIndex = DetectHeaderRowIndex(matrix);
        var headers = matrix[headerRowIndex].Select(NormalizeHeaderKey).ToList();

        if (headers.All(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                $"No se pudieron detectar encabezados de columnas en '{nombreArchivo}'.");
        }

        var dataRows = new List<IReadOnlyDictionary<string, string>>();
        for (var i = headerRowIndex + 1; i < matrix.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = matrix[i];
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count && c < row.Count; c++)
            {
                var key = headers[c];
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!dict.ContainsKey(key))
                    dict[key] = row[c].Trim();
            }

            if (dict.Values.All(string.IsNullOrWhiteSpace)) continue;

            dataRows.Add(dict);
        }

        return (headerRowIndex, headers, dataRows);
    }

    private static Sheet? SelectSheet(
        WorkbookPart workbookPart,
        IReadOnlyList<Sheet> sheets,
        string? preferredSheetName)
    {
        if (!string.IsNullOrWhiteSpace(preferredSheetName))
        {
            var want = preferredSheetName.Trim();
            foreach (var s in sheets)
            {
                var name = s.Name?.Value ?? string.Empty;
                if (name.Equals(want, StringComparison.OrdinalIgnoreCase))
                    return s;
            }
        }

        foreach (var s in sheets)
        {
            if (s.Id?.Value is not string relId) continue;
            var part = (WorksheetPart)workbookPart.GetPartById(relId);
            if (part.Worksheet?.Descendants<Row>().Any() == true)
                return s;
        }

        return sheets.Count > 0 ? sheets[0] : null;
    }

    private static void TrimTrailingEmptyRows(List<List<string>> matrix)
    {
        while (matrix.Count > 0 && RowIsEmpty(matrix[^1]))
        {
            matrix.RemoveAt(matrix.Count - 1);
        }
    }

    private static bool RowIsEmpty(IReadOnlyList<string> row) =>
        row.All(static c => string.IsNullOrWhiteSpace(c));

    private static int DetectHeaderRowIndex(IReadOnlyList<IReadOnlyList<string>> matrix)
    {
        var limit = Math.Min(matrix.Count, 15);
        var bestIdx = 0;
        var bestScore = -1;

        for (var i = 0; i < limit; i++)
        {
            var score = ScoreHeaderCandidate(matrix[i]);
            if (score > bestScore)
            {
                bestScore = score;
                bestIdx = i;
            }
        }

        if (bestScore < 1)
        {
            throw new InvalidOperationException(
                "Ninguna fila inicial parece contener encabezados reconocibles " +
                "(codigo, etapa, aplica, url, etc.).");
        }

        return bestIdx;
    }

    private static int ScoreHeaderCandidate(IReadOnlyList<string> row)
    {
        var score = 0;
        foreach (var cell in row)
        {
            if (string.IsNullOrWhiteSpace(cell)) continue;

            var n = NormalizeForHint(cell);
            if (n.Length == 0) continue;

            foreach (var hint in HeaderHintFragments)
            {
                if (n.Contains(hint, StringComparison.OrdinalIgnoreCase))
                {
                    score += 2;
                    break;
                }
            }
        }

        return score;
    }

    private static string NormalizeForHint(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch is ' ' or '-' or '_')
                sb.Append(ch);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Normaliza un encabezado de columna: sin tildes, minúsculas, espacios colapsados
    /// (para coincidir alias entre planillas del cliente y nombres esperados).
    /// </summary>
    public static string NormalizeHeaderKey(string header)
    {
        var trimmed = header.Trim();
        if (trimmed.Length == 0) return string.Empty;

        var decomposed = trimmed.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var folded = sb.ToString().ToLowerInvariant();
        var parts = folded.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries);

        return string.Join(' ', parts);
    }

    private static Stream EnsureSeekable(Stream src)
    {
        if (src.CanSeek) return src;

        var ms = new MemoryStream();
        src.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }
}
