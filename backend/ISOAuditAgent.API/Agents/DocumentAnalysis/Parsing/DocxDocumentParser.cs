using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ISOAuditAgent.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Parser de Word (.docx): <b>Fase D</b> — detecta secciones por estilos de
/// encabezado (<c>Heading1–3</c>, <c>OutlineLevel</c>, variantes en español).
/// No produce texto completo (<see cref="DocumentParseResult.TextoNormalizado"/> = <c>null</c>).
/// </summary>
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

            var secciones = ExtractSections(body, cancellationToken);

            _logger.LogDebug(
                "DocxDocumentParser: {Archivo} → {Secciones} secciones.",
                nombreArchivo, secciones.Count);

            return Task.FromResult(new DocumentParseResult(
                FormatoContenido.PlainText,
                secciones,
                TextoNormalizado: null));
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

    private static List<SeccionDetectada> ExtractSections(Body body, CancellationToken cancellationToken)
    {
        var result = new List<SeccionDetectada>();
        string? pendingTitle = null;
        var pendingBody = false;

        void Flush()
        {
            if (pendingTitle is null && !pendingBody) return;
            var rawTitle = pendingTitle ?? "(Contenido inicial)";
            var normTitle = TextNormalizer.Normalize(rawTitle.Trim());
            if (string.IsNullOrEmpty(normTitle) && !pendingBody) return;
            result.Add(new SeccionDetectada(
                string.IsNullOrEmpty(normTitle) ? "(Contenido inicial)" : normTitle,
                pendingBody));
        }

        foreach (var child in body.ChildElements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (child)
            {
                case Paragraph p:
                    if (IsHeadingParagraph(p))
                    {
                        Flush();
                        pendingTitle = GetParagraphText(p);
                        pendingBody = false;
                    }
                    else if (ParagraphHasMeaningfulText(p))
                        pendingBody = true;

                    break;

                case Table t:
                    if (TableHasMeaningfulText(t))
                        pendingBody = true;
                    break;
            }
        }

        Flush();
        return result;
    }

    private static bool IsHeadingParagraph(Paragraph p)
    {
        var pPr = p.ParagraphProperties;
        if (pPr?.OutlineLevel?.Val is not null)
            return true;

        var styleId = pPr?.ParagraphStyleId?.Val?.Value;
        if (styleId is null) return false;

        if (styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
            return true;

        if (styleId.StartsWith("Titulo", StringComparison.OrdinalIgnoreCase))
            return true;

        // Plantillas ES a veces usan "Encabezado1" (Word ES)
        return styleId.StartsWith("Encabezado", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetParagraphText(Paragraph p)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var run in p.Descendants<Run>())
        {
            foreach (var t in run.Elements<Text>())
                sb.Append(t.Text);

            if (run.Elements<Break>().Any())
                sb.Append('\n');
        }

        return sb.ToString().Trim();
    }

    private static bool ParagraphHasMeaningfulText(Paragraph p)
    {
        var t = GetParagraphText(p);
        return t.Length > 0;
    }

    private static bool TableHasMeaningfulText(Table table)
    {
        return table.Descendants<Text>().Any(x => !string.IsNullOrWhiteSpace(x.Text));
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
