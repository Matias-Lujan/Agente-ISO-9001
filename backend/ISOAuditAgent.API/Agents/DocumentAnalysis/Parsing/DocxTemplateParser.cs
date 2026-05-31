// ============================================================================
//  DocxTemplateParser — Parser de .docx con DocumentFormat.OpenXml (D3.3)
// ----------------------------------------------------------------------------
//  Estrategia: las secciones se detectan por párrafos con estilo "heading":
//   - OutlineLevel definido, o
//   - ParagraphStyleId que empieza con Heading / Titulo / Encabezado
//     (variantes EN y ES de Word).
//
//  TieneContenido = hay al menos un párrafo o tabla con texto entre ese
//  heading y el siguiente.
//
//  Lógica adaptada del DocxDocumentParser del compañero, simplificada al
//  contrato actual (SeccionDetectada: Titulo + TieneContenido). Sin
//  TextNormalizer, sin OpenXmlPackageDiagnostics — esas piezas son
//  mejoras incrementales que se evalúan si aparecen problemas reales en
//  D3.4 o D3.5.
// ============================================================================

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ISOAuditAgent.API.Agents.Contracts;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;

public sealed class DocxTemplateParser : ITemplateParser
{
    public IReadOnlyList<SeccionDetectada> Parsear(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException(
                "DocxTemplateParser: el archivo está vacío (0 bytes).");
        }

        try
        {
            // MemoryStream sobre los bytes — OpenXml necesita un Stream seekable.
            using var ms = new MemoryStream(bytes, writable: false);
            using var doc = WordprocessingDocument.Open(ms, isEditable: false);

            var body = doc.MainDocumentPart?.Document?.Body
                ?? throw new InvalidOperationException(
                    "El paquete OpenXml es válido pero no contiene " +
                    "MainDocumentPart/Body.");

            return ExtraerSecciones(body);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error parseando DOCX: {ex.Message}", ex);
        }
    }

    private static IReadOnlyList<SeccionDetectada> ExtraerSecciones(Body body)
    {
        var resultado = new List<SeccionDetectada>();
        string? tituloPendiente = null;
        var hayContenidoPendiente = false;

        void Flush()
        {
            if (tituloPendiente is null && !hayContenidoPendiente)
                return;

            var titulo = string.IsNullOrWhiteSpace(tituloPendiente)
                ? "(Contenido inicial)"
                : tituloPendiente.Trim();

            resultado.Add(new SeccionDetectada(titulo, hayContenidoPendiente));
        }

        foreach (var child in body.ChildElements)
        {
            switch (child)
            {
                case Paragraph p:
                    if (EsHeading(p))
                    {
                        Flush();
                        tituloPendiente = TextoDelParrafo(p);
                        hayContenidoPendiente = false;
                    }
                    else if (ParrafoTieneTexto(p))
                    {
                        hayContenidoPendiente = true;
                    }
                    break;

                case Table t:
                    if (TablaTieneTexto(t))
                        hayContenidoPendiente = true;
                    break;
            }
        }

        Flush();
        return resultado;
    }

    private static bool EsHeading(Paragraph p)
    {
        var pPr = p.ParagraphProperties;
        if (pPr?.OutlineLevel?.Val is not null) return true;

        var styleId = pPr?.ParagraphStyleId?.Val?.Value;
        if (string.IsNullOrEmpty(styleId)) return false;

        return styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase)
            || styleId.StartsWith("Titulo", StringComparison.OrdinalIgnoreCase)
            || styleId.StartsWith("Encabezado", StringComparison.OrdinalIgnoreCase);
    }

    private static string TextoDelParrafo(Paragraph p)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var run in p.Descendants<Run>())
        {
            foreach (var t in run.Elements<Text>())
                sb.Append(t.Text);
        }
        return sb.ToString().Trim();
    }

    private static bool ParrafoTieneTexto(Paragraph p) =>
        TextoDelParrafo(p).Length > 0;

    private static bool TablaTieneTexto(Table table) =>
        table.Descendants<Text>().Any(x => !string.IsNullOrWhiteSpace(x.Text));
}