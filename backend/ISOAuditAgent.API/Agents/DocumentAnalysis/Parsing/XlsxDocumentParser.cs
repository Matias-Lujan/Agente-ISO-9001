using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ISOAuditAgent.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Parser Excel (<b>Fase D</b>): cada hoja con al menos una fila se expone
/// como una <see cref="SeccionDetectada"/> titulada con el nombre de la hoja;
/// <see cref="SeccionDetectada.TieneContenido"/> es <c>true</c> si hay más
/// de una fila (p. ej. cabecera + datos).
/// </summary>
/// <remarks>
/// Ya no genera Markdown ni <see cref="DocumentParseResult.TextoNormalizado"/>.
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

            var sheets = workbookPart.Workbook?.Sheets?.OfType<Sheet>().ToList()
                         ?? new List<Sheet>();

            var secciones = new List<SeccionDetectada>();

            foreach (var sheet in sheets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (sheet.Id?.Value is not string relId) continue;

                var part = (WorksheetPart)workbookPart.GetPartById(relId);
                if (part.Worksheet is null) continue;

                var rows = part.Worksheet.Descendants<Row>().ToList();
                if (rows.Count == 0) continue;

                var sheetName = sheet.Name?.Value?.Trim();
                if (string.IsNullOrEmpty(sheetName))
                    sheetName = "Hoja";

                var titulo = TextNormalizer.Normalize(sheetName);
                var tieneContenido = rows.Count > 1;

                secciones.Add(new SeccionDetectada(titulo, tieneContenido));
            }

            _logger.LogDebug(
                "XlsxDocumentParser: {Archivo} → {Hojas} secciones (hojas no vacías).",
                nombreArchivo, secciones.Count);

            return Task.FromResult(new DocumentParseResult(
                FormatoContenido.Markdown,
                secciones,
                TextoNormalizado: null));
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

    private static Stream EnsureSeekable(Stream src)
    {
        if (src.CanSeek) return src;

        var ms = new MemoryStream();
        src.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }
}
