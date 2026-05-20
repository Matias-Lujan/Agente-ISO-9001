using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocxBody = DocumentFormat.OpenXml.Wordprocessing.Body;
using DocxDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using DocxParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using DocxParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using DocxRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using DocxText = DocumentFormat.OpenXml.Wordprocessing.Text;
using ParagraphStyleId = DocumentFormat.OpenXml.Wordprocessing.ParagraphStyleId;
using XlsCell = DocumentFormat.OpenXml.Spreadsheet.Cell;
using XlsCellValue = DocumentFormat.OpenXml.Spreadsheet.CellValue;
using XlsCellValues = DocumentFormat.OpenXml.Spreadsheet.CellValues;
using XlsRow = DocumentFormat.OpenXml.Spreadsheet.Row;
using XlsSheet = DocumentFormat.OpenXml.Spreadsheet.Sheet;
using XlsSheetData = DocumentFormat.OpenXml.Spreadsheet.SheetData;
using XlsSheets = DocumentFormat.OpenXml.Spreadsheet.Sheets;
using XlsWorkbook = DocumentFormat.OpenXml.Spreadsheet.Workbook;
using XlsWorksheet = DocumentFormat.OpenXml.Spreadsheet.Worksheet;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using PdfPoint = UglyToad.PdfPig.Core.PdfPoint;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Parsing.Fixtures;

/// <summary>
/// Genera fixtures binarios de PDF / DOCX / XLSX con contenido conocido
/// para los tests de los parsers de Fase 4. Mantener los fixtures
/// generados en código (no como archivos versionados) deja los tests
/// reproducibles, sin binarios en el repo, y permite ajustar el
/// contenido caso a caso.
/// </summary>
internal static class DocumentFixtures
{
    public static byte[] BuildPdf(params string[] paragraphs)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(595, 842);

        var y = 800.0;
        foreach (var line in paragraphs)
        {
            page.AddText(line, 12, new PdfPoint(50, y), font);
            y -= 20;
        }

        return builder.Build();
    }

    public static byte[] BuildDocx(params string[] paragraphs)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(
                   ms, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            var body = new DocxBody();
            foreach (var p in paragraphs)
            {
                body.Append(new DocxParagraph(new DocxRun(new DocxText(p))));
            }
            main.Document = new DocxDocument(body);
            main.Document.Save();
        }

        return ms.ToArray();
    }

    /// <summary>
    /// DOCX con <c>w:pStyle</c> opcional por párrafo (p. ej. <c>Heading1</c>).
    /// </summary>
    public static byte[] BuildDocxWithStyledParagraphs(
        IReadOnlyList<(string? styleId, string text)> blocks)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(
                   ms, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            var body = new DocxBody();
            foreach (var (styleId, text) in blocks)
            {
                var p = new DocxParagraph();
                if (!string.IsNullOrEmpty(styleId))
                {
                    p.ParagraphProperties = new DocxParagraphProperties(
                        new ParagraphStyleId { Val = styleId });
                }

                p.Append(new DocxRun(new DocxText(text)));
                body.Append(p);
            }

            main.Document = new DocxDocument(body);
            main.Document.Save();
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Genera un XLSX usando inline strings (sin SharedStringTable),
    /// lo cual es más simple de armar y el parser igualmente lo soporta.
    /// </summary>
    public static byte[] BuildXlsx(
        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>> sheetsContent)
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(
                   ms, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new XlsWorkbook();
            var sheets = workbookPart.Workbook.AppendChild(new XlsSheets());

            uint sheetId = 1;
            foreach (var (sheetName, rows) in sheetsContent)
            {
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();

                var sheetData = new XlsSheetData();
                foreach (var rowValues in rows)
                {
                    var row = new XlsRow();
                    foreach (var value in rowValues)
                    {
                        row.Append(InlineCell(value));
                    }
                    sheetData.Append(row);
                }

                worksheetPart.Worksheet = new XlsWorksheet(sheetData);

                sheets.Append(new XlsSheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = sheetId++,
                    Name = sheetName
                });
            }

            workbookPart.Workbook.Save();
        }

        return ms.ToArray();
    }

    private static XlsCell InlineCell(string value)
    {
        var cell = new XlsCell { DataType = XlsCellValues.InlineString };
        var inline = new DocumentFormat.OpenXml.Spreadsheet.InlineString(
            new DocumentFormat.OpenXml.Spreadsheet.Text(value));
        cell.AppendChild(inline);
        return cell;
    }

    public static MemoryStream ToStream(byte[] bytes) =>
        new(bytes, writable: false);
}
