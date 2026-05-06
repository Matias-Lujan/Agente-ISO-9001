using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ISOAuditAgent.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Parser de Excel (.xlsx) basado en <c>DocumentFormat.OpenXml</c>.
/// Renderiza cada hoja del libro como una <b>tabla Markdown</b>
/// (criterio de aceptación de Fase 4: "Excel como Markdown" según
/// <see cref="FormatoContenido.Markdown"/>).
/// </summary>
/// <remarks>
/// <para>Convenciones de salida:</para>
/// <list type="bullet">
///   <item><description>
///     Encabezado <c>"## NombreHoja"</c> antes de cada tabla.
///   </description></item>
///   <item><description>
///     Primera fila de datos tratada como header (común en QMS).
///     Si la hoja tiene una sola fila, no se agrega separador.
///   </description></item>
///   <item><description>
///     Hojas vacías se omiten.
///   </description></item>
///   <item><description>
///     Caracteres conflictivos en celdas (<c>|</c>, salto de línea) se
///     escapan: <c>|</c> → <c>\|</c>, <c>\n</c> → <c>&lt;br&gt;</c>.
///   </description></item>
/// </list>
/// </remarks>
public sealed class XlsxDocumentParser : IDocumentParser
{
    private static readonly IReadOnlyCollection<string> Mimes = new[]
    {
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    private readonly ILogger<XlsxDocumentParser> _logger;

    public XlsxDocumentParser(ILogger<XlsxDocumentParser>? logger = null)
    {
        _logger = logger ?? NullLogger<XlsxDocumentParser>.Instance;
    }

    public IReadOnlyCollection<string> SupportedMimeTypes => Mimes;

    public Task<DocumentParseResult> ParseAsync(
        Stream contenido,
        string nombreArchivo,
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
                    "El paquete OpenXml es válido pero no contiene WorkbookPart " +
                    "(estructura interna corrupta).");

            var sharedStrings = LoadSharedStrings(workbookPart);
            var sheets = workbookPart.Workbook?.Sheets?.OfType<Sheet>().ToList()
                         ?? new List<Sheet>();

            var sb = new StringBuilder();

            foreach (var sheet in sheets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (sheet.Id?.Value is not string relId) continue;

                var part = (WorksheetPart)workbookPart.GetPartById(relId);
                if (part.Worksheet is null) continue;

                var rows = part.Worksheet.Descendants<Row>().ToList();
                if (rows.Count == 0) continue;

                if (sb.Length > 0) sb.Append('\n');
                sb.Append("## ").Append(sheet.Name?.Value ?? "Hoja").Append('\n');

                AppendMarkdownTable(rows, sharedStrings, sb, cancellationToken);
            }

            var normalizado = TextNormalizer.Normalize(sb.ToString());

            _logger.LogDebug(
                "XlsxDocumentParser: {Archivo} → {Hojas} hojas, {Caracteres} car normalizados.",
                nombreArchivo, sheets.Count, normalizado.Length);

            return Task.FromResult(new DocumentParseResult(
                normalizado, FormatoContenido.Markdown));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var details = OpenXmlPackageDiagnostics.DescribeFailure(
                ex, seekable, nombreArchivo,
                "XLSX (OpenXml SpreadsheetML)");

            throw new InvalidOperationException(
                $"Error parseando XLSX: {details}", ex);
        }
    }

    private static void AppendMarkdownTable(
        List<Row> rows,
        IReadOnlyList<string> sharedStrings,
        StringBuilder sb,
        CancellationToken cancellationToken)
    {
        var matrix = new List<List<string>>(rows.Count);
        var maxCols = 0;

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cells = new List<string>();
            foreach (var cell in row.Elements<Cell>())
            {
                cells.Add(EscapeMarkdownCell(ResolveCellValue(cell, sharedStrings)));
            }

            if (cells.Count > maxCols) maxCols = cells.Count;
            matrix.Add(cells);
        }

        if (maxCols == 0) return;

        foreach (var row in matrix)
        {
            while (row.Count < maxCols) row.Add(string.Empty);
        }

        AppendRow(sb, matrix[0]);

        if (matrix.Count > 1)
        {
            sb.Append('|');
            for (var c = 0; c < maxCols; c++) sb.Append("---|");
            sb.Append('\n');

            for (var i = 1; i < matrix.Count; i++)
            {
                AppendRow(sb, matrix[i]);
            }
        }
    }

    private static void AppendRow(StringBuilder sb, List<string> cells)
    {
        sb.Append('|');
        foreach (var c in cells)
        {
            sb.Append(' ').Append(c).Append(" |");
        }
        sb.Append('\n');
    }

    private static string EscapeMarkdownCell(string value) =>
        value
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal)
            .Replace("\r", "<br>", StringComparison.Ordinal);

    private static string ResolveCellValue(Cell cell, IReadOnlyList<string> sharedStrings)
    {
        var raw = cell.CellValue?.Text ?? cell.InnerText ?? string.Empty;

        if (cell.DataType?.Value == CellValues.SharedString
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx)
            && idx >= 0 && idx < sharedStrings.Count)
        {
            return sharedStrings[idx];
        }

        if (cell.DataType?.Value == CellValues.Boolean)
        {
            return raw == "1" ? "true" : "false";
        }

        if (cell.DataType?.Value == CellValues.InlineString)
        {
            return cell.InlineString?.Text?.Text ?? raw;
        }

        return raw;
    }

    private static IReadOnlyList<string> LoadSharedStrings(WorkbookPart workbookPart)
    {
        var part = workbookPart.SharedStringTablePart;
        if (part?.SharedStringTable is null)
        {
            return Array.Empty<string>();
        }

        return part.SharedStringTable
            .Elements<SharedStringItem>()
            .Select(s => s.InnerText)
            .ToList();
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
