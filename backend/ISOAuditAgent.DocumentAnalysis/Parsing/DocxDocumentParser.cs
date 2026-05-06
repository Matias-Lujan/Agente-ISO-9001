using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ISOAuditAgent.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Parser de Word (.docx) basado en <c>DocumentFormat.OpenXml</c>
/// (oficial de Microsoft, MIT). Recorre el cuerpo del documento y
/// concatena el texto de cada párrafo separado por <c>\n</c>; tablas
/// se renderizan como filas con celdas separadas por tab.
/// </summary>
/// <remarks>
/// No interpreta estilos, imágenes ni headers/footers complejos: el
/// objetivo es texto plano fiel al contenido legible. Para los casos
/// donde RAG necesite preservar estructura (encabezados, listas) se
/// puede extender más adelante a Markdown sin tocar el contrato.
/// </remarks>
public sealed class DocxDocumentParser : IDocumentParser
{
    private static readonly IReadOnlyCollection<string> Mimes = new[]
    {
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private readonly ILogger<DocxDocumentParser> _logger;

    public DocxDocumentParser(ILogger<DocxDocumentParser>? logger = null)
    {
        _logger = logger ?? NullLogger<DocxDocumentParser>.Instance;
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

            using var doc = WordprocessingDocument.Open(seekable, isEditable: false);

            var body = doc.MainDocumentPart?.Document?.Body
                       ?? throw new InvalidOperationException(
                           "El paquete OpenXml es válido pero no contiene " +
                           "MainDocumentPart/Body (estructura interna corrupta o " +
                           "exportación incompleta).");

            var sb = new StringBuilder();
            AppendOpenXmlElement(body, sb, cancellationToken);

            var normalizado = TextNormalizer.Normalize(sb.ToString());

            _logger.LogDebug(
                "DocxDocumentParser: {Archivo} → {Caracteres} car normalizados.",
                nombreArchivo, normalizado.Length);

            return Task.FromResult(new DocumentParseResult(
                normalizado, FormatoContenido.PlainText));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var details = OpenXmlPackageDiagnostics.DescribeFailure(
                ex, seekable, nombreArchivo,
                "DOCX (OpenXml WordprocessingML)");

            throw new InvalidOperationException(
                $"Error parseando DOCX: {details}", ex);
        }
    }

    private static void AppendOpenXmlElement(
        DocumentFormat.OpenXml.OpenXmlElement element,
        StringBuilder sb,
        CancellationToken cancellationToken)
    {
        foreach (var child in element.ChildElements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (child)
            {
                case Paragraph p:
                    AppendParagraph(p, sb);
                    sb.Append('\n');
                    break;

                case Table t:
                    AppendTable(t, sb);
                    break;

                default:
                    AppendOpenXmlElement(child, sb, cancellationToken);
                    break;
            }
        }
    }

    private static void AppendParagraph(Paragraph p, StringBuilder sb)
    {
        foreach (var run in p.Descendants<Run>())
        {
            foreach (var t in run.Elements<Text>())
            {
                sb.Append(t.Text);
            }

            if (run.Elements<Break>().Any())
            {
                sb.Append('\n');
            }

            if (run.Elements<TabChar>().Any())
            {
                sb.Append('\t');
            }
        }
    }

    private static void AppendTable(Table table, StringBuilder sb)
    {
        foreach (var row in table.Elements<TableRow>())
        {
            var first = true;
            foreach (var cell in row.Elements<TableCell>())
            {
                if (!first) sb.Append('\t');
                first = false;

                var cellText = string.Concat(cell
                    .Descendants<Text>()
                    .Select(x => x.Text));
                sb.Append(cellText);
            }
            sb.Append('\n');
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
