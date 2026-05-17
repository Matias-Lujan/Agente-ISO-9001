using System.Globalization;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Resolución de valores de celdas SpreadsheetML reutilizada por
/// <see cref="XlsxDocumentParser"/> y <see cref="XlsxTableExtractor"/>.
/// </summary>
internal static class SpreadsheetCellValueResolver
{
    public static IReadOnlyList<string> LoadSharedStrings(WorkbookPart workbookPart)
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

    public static string ResolveCellValue(Cell cell, IReadOnlyList<string> sharedStrings)
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
}
