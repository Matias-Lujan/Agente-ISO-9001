using ISOAuditAgent.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Outline;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Parser PDF (<b>Fase D</b>): intenta secciones vía <see cref="Bookmarks"/>;
/// si no hay outline, aplica heurística por tamaño de letra (bajo recall en
/// PDFs sin jerarquía tipográfica clara — ver
/// <see cref="PdfTypographySectionHeuristic"/>).
/// </summary>
/// <remarks>
/// No produce <see cref="DocumentParseResult.TextoNormalizado"/>; el resultado
/// útil son las <see cref="SeccionDetectada"/>.
/// </remarks>
public sealed class PdfDocumentParser : IDocumentParser
{
    private static readonly IReadOnlyCollection<string> Mimes =
        new[] { "application/pdf" };

    private readonly ILogger<PdfDocumentParser> _logger;

    public PdfDocumentParser(ILogger<PdfDocumentParser>? logger = null)
    {
        _logger = logger ?? NullLogger<PdfDocumentParser>.Instance;
    }

    public IReadOnlyCollection<string> SupportedMimeTypes => Mimes;

    public Task<DocumentParseResult> ParseAsync(
        Stream contenido,
        string nombreArchivo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contenido);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var pdf = PdfDocument.Open(contenido, new ParsingOptions { UseLenientParsing = true });

            IReadOnlyList<SeccionDetectada> secciones;
            if (pdf.TryGetBookmarks(out var bookmarks)
                && bookmarks.Roots.Count > 0)
            {
                var outline = CollectBookmarkSections(bookmarks, cancellationToken);
                secciones = outline.Count > 0
                    ? outline
                    : PdfTypographySectionHeuristic.Detect(pdf, cancellationToken);
            }
            else
                secciones = PdfTypographySectionHeuristic.Detect(pdf, cancellationToken);

            _logger.LogDebug(
                "PdfDocumentParser: {Archivo} → {Paginas} pág, {Secciones} secciones.",
                nombreArchivo, pdf.NumberOfPages, secciones.Count);

            return Task.FromResult(new DocumentParseResult(
                FormatoContenido.PlainText,
                secciones,
                TextoNormalizado: null));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Error parseando PDF '{nombreArchivo}': {ex.Message}", ex);
        }
    }

    private static List<SeccionDetectada> CollectBookmarkSections(
        Bookmarks bookmarks,
        CancellationToken cancellationToken)
    {
        var list = new List<SeccionDetectada>();
        foreach (var root in bookmarks.Roots)
            VisitBookmark(root, list, cancellationToken);
        return list;
    }

    private static void VisitBookmark(
        BookmarkNode node,
        List<SeccionDetectada> sink,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var t = TextNormalizer.Normalize(node.Title?.Trim() ?? string.Empty);
        if (!string.IsNullOrEmpty(t))
        {
            // Hoja del outline: en general implica destino con contenido;
            // nodo contenedor sin hoja suele ser solo agrupador.
            sink.Add(new SeccionDetectada(t, node.IsLeaf));
        }

        foreach (var child in node.Children)
            VisitBookmark(child, sink, cancellationToken);
    }
}
