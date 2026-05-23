// ============================================================================
//  PdfTemplateParser — Parser de PDF con PdfPig (D3.3)
// ----------------------------------------------------------------------------
//  Estrategia heurística: detectar headings por TAMAÑO DE FUENTE. Los PDFs
//  no tienen estructura semántica como DOCX — son cajas de texto sobre una
//  página. Un heurístico simple:
//
//    1. Calcular la fontSize "típica" del documento (media o mediana).
//    2. Una línea es candidata a heading si su fontSize > media * factor
//       Y tiene poco texto (1-2 palabras o frase corta).
//    3. Las líneas que siguen al heading hasta el próximo heading o fin de
//       documento aportan al TieneContenido.
//
//  LIMITACIONES CONOCIDAS:
//   - PDFs con tipografía uniforme (todo mismo tamaño) van a devolver una
//     sola sección "(Contenido inicial)".
//   - PDFs generados desde scans (sin texto) van a devolver lista vacía.
//   - PDFs con tablas grandes pueden tener falsos positivos.
//
//  ACEPTABLE PARA MVP: el sistema audita estructura, no contenido. Si un
//  PDF no expone secciones claras, el ConsistencyVerification simplemente
//  no detectará secciones vacías ahí — no rompe nada.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using System.Diagnostics.Metrics;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;

public sealed class PdfTemplateParser : ITemplateParser
{
    /// <summary>
    /// Factor sobre la mediana de fontSize para considerar una línea heading.
    /// 1.15 = 15% más grande. Conservador para no marcar texto ligeramente
    /// más grande (negritas inline) como heading.
    /// </summary>
    private const double FactorHeading = 1.15;

    /// <summary>
    /// Máxima cantidad de palabras para considerar una línea heading.
    /// Headings típicos son cortos ("1. Introducción", "Conclusiones").
    /// </summary>
    private const int MaxPalabrasHeading = 12;

    public IReadOnlyList<SeccionDetectada> Parsear(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException(
                "PdfTemplateParser: el archivo está vacío (0 bytes).");
        }

        try
        {
            using var pdf = PdfDocument.Open(bytes);

            // 1. Recolectar todas las líneas con su fontSize promedio.
            var lineas = new List<(string Texto, double FontSize)>();
            foreach (var page in pdf.GetPages())
            {
                foreach (var word in AgruparLineasDePagina(page))
                {
                    if (!string.IsNullOrWhiteSpace(word.Texto))
                        lineas.Add(word);
                }
            }

            if (lineas.Count == 0)
                return Array.Empty<SeccionDetectada>();

            // 2. Calcular la mediana de fontSize.
            var mediana = MedianaFontSize(lineas);
            var umbralHeading = mediana * FactorHeading;

            // 3. Recorrer las líneas marcando headings y acumulando contenido.
            return ExtraerSeccionesDeLineas(lineas, umbralHeading);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error parseando PDF: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Agrupa las letras de una página en "líneas" aproximadas usando la
    /// coordenada Y de baseline. Devuelve para cada línea el texto y el
    /// fontSize promedio.
    /// </summary>
    private static IEnumerable<(string Texto, double FontSize)> AgruparLineasDePagina(Page page)
    {
        // Tolerancia vertical para considerar dos letras en la misma línea.
        const double tolerancia = 2.0;

        var letras = page.Letters
            .Where(l => !string.IsNullOrEmpty(l.Value))
            .OrderByDescending(l => l.GlyphRectangle.Bottom)
            .ThenBy(l => l.GlyphRectangle.Left)
            .ToList();

        if (letras.Count == 0) yield break;

        var lineaActual = new List<Letter> { letras[0] };
        var baselineActual = letras[0].GlyphRectangle.Bottom;

        for (int i = 1; i < letras.Count; i++)
        {
            var l = letras[i];
            if (Math.Abs(l.GlyphRectangle.Bottom - baselineActual) < tolerancia)
            {
                lineaActual.Add(l);
            }
            else
            {
                yield return ConstruirLinea(lineaActual);
                lineaActual = new List<Letter> { l };
                baselineActual = l.GlyphRectangle.Bottom;
            }
        }

        yield return ConstruirLinea(lineaActual);
    }

    private static (string Texto, double FontSize) ConstruirLinea(List<Letter> letras)
    {
        var ordenadas = letras.OrderBy(l => l.GlyphRectangle.Left).ToList();
        var texto = string.Concat(ordenadas.Select(l => l.Value)).Trim();
        var fontSize = ordenadas.Average(l => l.PointSize);
        return (texto, fontSize);
    }

    private static double MedianaFontSize(IReadOnlyList<(string Texto, double FontSize)> lineas)
    {
        var ordenados = lineas.Select(l => l.FontSize).OrderBy(x => x).ToList();
        var n = ordenados.Count;
        return n % 2 == 1
            ? ordenados[n / 2]
            : (ordenados[n / 2 - 1] + ordenados[n / 2]) / 2.0;
    }

    private static IReadOnlyList<SeccionDetectada> ExtraerSeccionesDeLineas(
        IReadOnlyList<(string Texto, double FontSize)> lineas, double umbralHeading)
    {
        var resultado = new List<SeccionDetectada>();
        string? tituloPendiente = null;
        var hayContenidoPendiente = false;

        void Flush()
        {
            if (tituloPendiente is null && !hayContenidoPendiente) return;

            var titulo = string.IsNullOrWhiteSpace(tituloPendiente)
                ? "(Contenido inicial)"
                : tituloPendiente.Trim();

            resultado.Add(new SeccionDetectada(titulo, hayContenidoPendiente));
        }

        foreach (var (texto, fontSize) in lineas)
        {
            var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            var esHeading = fontSize >= umbralHeading && palabras <= MaxPalabrasHeading;

            if (esHeading)
            {
                Flush();
                tituloPendiente = texto;
                hayContenidoPendiente = false;
            }
            else
            {
                hayContenidoPendiente = true;
            }
        }

        Flush();
        return resultado;
    }
}