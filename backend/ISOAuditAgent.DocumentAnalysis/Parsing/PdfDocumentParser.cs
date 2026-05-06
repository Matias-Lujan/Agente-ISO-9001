using ISOAuditAgent.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Parser de PDF usando <c>UglyToad.PdfPig</c>. Extrae el texto página
/// por página, separa páginas con doble salto de línea y normaliza el
/// resultado.
/// </summary>
/// <remarks>
/// PdfPig es una librería 100% managed (no requiere binarios nativos),
/// MIT, y maneja la mayoría de PDFs de oficina razonablemente bien.
/// Para PDFs basados en imágenes (escaneos sin OCR) el texto extraído
/// será vacío — eso queda como decisión del runner (Fase 5) sobre cómo
/// reaccionar (omitir documento + log).
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
            using var pdf = PdfDocument.Open(contenido);

            var texto = ExtractText(pdf, cancellationToken);

            var normalizado = TextNormalizer.Normalize(texto);

            _logger.LogDebug(
                "PdfDocumentParser: {Archivo} → {Paginas} pág, {Caracteres} car normalizados.",
                nombreArchivo, pdf.NumberOfPages, normalizado.Length);

            return Task.FromResult(new DocumentParseResult(
                normalizado, FormatoContenido.PlainText));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Error parseando PDF '{nombreArchivo}': {ex.Message}", ex);
        }
    }

    private static string ExtractText(PdfDocument pdf, CancellationToken cancellationToken)
    {
        var pages = new List<string>(pdf.NumberOfPages);
        for (var i = 1; i <= pdf.NumberOfPages; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Page page = pdf.GetPage(i);
            pages.Add(page.Text);
        }

        return string.Join("\n\n", pages);
    }
}
