using ISOAuditAgent.Contracts;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Detección de secciones por tamaño de fuente cuando el PDF no tiene
/// outline/bookmarks. Recall limitado: documentado en
/// <see cref="PdfDocumentParser"/>.
/// </summary>
internal static class PdfTypographySectionHeuristic
{
    private const double SizeRatioHeading = 1.18;
    private const int MaxHeadingLength = 120;

    /// <summary>
    /// Agrupa letras en líneas por página, estima el tamaño corporativo
    /// mediano global y trata como encabezados las líneas notablemente más
    /// grandes y cortas, en orden de lectura (página ↑, dentro de página ↓ Y).
    /// </summary>
    public static IReadOnlyList<SeccionDetectada> Detect(PdfDocument pdf, CancellationToken cancellationToken)
    {
        var orderedLines = new List<(int Page, double Y, string Text, double AvgPointSize)>();

        for (var p = 1; p <= pdf.NumberOfPages; p++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = pdf.GetPage(p);
            foreach (var line in GroupPageIntoLines(page))
                orderedLines.Add((p, line.BaselineY, line.Text, line.AvgPointSize));
        }

        if (orderedLines.Count == 0)
            return SingleSectionFromFullText(pdf, cancellationToken);

        var sizes = orderedLines
            .Select(l => l.AvgPointSize)
            .Where(s => s > 0.1)
            .OrderBy(s => s)
            .ToList();

        if (sizes.Count == 0)
            return SingleSectionFromFullText(pdf, cancellationToken);

        var median = sizes[sizes.Count / 2];
        var headingThreshold = Math.Max(median * SizeRatioHeading, median + 1.2);

        var sorted = orderedLines
            .OrderBy(x => x.Page)
            .ThenByDescending(x => x.Y)
            .ToList();

        var sections = new List<SeccionDetectada>();
        string? currentTitle = null;
        var hasBody = false;

        void Flush()
        {
            if (currentTitle is null && !hasBody) return;
            var t = string.IsNullOrEmpty(currentTitle)
                ? "(Contenido inicial)"
                : TextNormalizer.Normalize(currentTitle!);
            if (string.IsNullOrEmpty(t) && !hasBody) return;
            sections.Add(new SeccionDetectada(
                string.IsNullOrEmpty(t) ? "(Contenido inicial)" : t,
                hasBody));
        }

        foreach (var (_, _, text, avgSize) in sorted)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var trimmed = text.Trim();
            if (trimmed.Length == 0) continue;

            var isHeading = avgSize >= headingThreshold
                && trimmed.Length <= MaxHeadingLength;

            if (isHeading)
            {
                Flush();
                currentTitle = trimmed;
                hasBody = false;
            }
            else
                hasBody = true;
        }

        Flush();

        return sections.Count > 0
            ? sections
            : SingleSectionFromFullText(pdf, cancellationToken);
    }

    private static List<(double BaselineY, string Text, double AvgPointSize)> GroupPageIntoLines(Page page)
    {
        var result = new List<(double, string, double)>();
        if (page.Letters.Count == 0) return result;

        const double rowTol = 2.0;
        var groups = page.Letters
            .GroupBy(l => Math.Round(l.StartBaseLine.Y / rowTol) * rowTol)
            .OrderByDescending(g => g.Key);

        foreach (var g in groups)
        {
            var ordered = g.OrderBy(l => l.StartBaseLine.X).ToList();
            if (ordered.Count == 0) continue;

            var text = string.Concat(ordered.Select(l => l.Value)).Trim();
            if (text.Length == 0) continue;

            var avg = ordered.Average(l =>
                l.PointSize > 0
                    ? l.PointSize
                    : (l.FontSize > 0 ? l.FontSize : l.GlyphRectangle.Height));

            if (avg <= 0)
                avg = ordered.Average(l => l.GlyphRectangle.Height);

            result.Add((g.Key, text, avg));
        }

        return result;
    }

    private static IReadOnlyList<SeccionDetectada> SingleSectionFromFullText(
        PdfDocument pdf,
        CancellationToken cancellationToken)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 1; i <= pdf.NumberOfPages; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append(pdf.GetPage(i).Text);
        }

        var norm = TextNormalizer.Normalize(sb.ToString());
        return
        [
            new SeccionDetectada("(documento completo)", !string.IsNullOrWhiteSpace(norm))
        ];
    }
}
