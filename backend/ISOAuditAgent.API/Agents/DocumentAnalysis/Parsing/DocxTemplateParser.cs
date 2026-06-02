// ============================================================================
//  DocxTemplateParser — Parser de .docx con DocumentFormat.OpenXml (D3.3)
// ----------------------------------------------------------------------------
//  Estrategia: secciones a dos niveles, producidas en paralelo.
//
//  NIVEL 1 — Párrafos con estilo heading:
//    - OutlineLevel definido, o
//    - ParagraphStyleId que empieza con Heading / Titulo / Encabezado
//      (variantes EN y ES de Word).
//    TieneContenido = hay al menos un párrafo o tabla con texto entre ese
//    heading y el siguiente.
//
//  NIVEL 2 — Tablas con estructura de campos:
//    Inmediatamente al encontrar una tabla se extraen SeccionDetectadas a
//    nivel de campo/columna (ver ExtraerSeccionesTabla). Esto detecta campos
//    vacíos en tablas de tipo label-valor o datos con encabezado, sin necesidad
//    de que exista un heading encima.
//
//  Las dos listas se combinan en la salida (heading-level + table-field-level).
//  La comparación documento↔template en HallazgosEstructurales usa
//  NormalizeHeaderKey para el matching, por lo que el orden no importa.
//
//  TABLAS — TRES ESTRUCTURAS CUBIERTAS:
//
//   A. 1 columna: primera fila = label de sección, filas siguientes = contenido.
//      SeccionDetectada(fila0, hayContenidoEnFilas1+).
//
//   B. 2 columnas: cada fila es un par label/valor.
//      SeccionDetectada(celda0, celda1TieneTexto) por cada fila con celda0 no vacía.
//
//   C. N columnas (N≥2), primera fila toda no vacía: tabla de datos.
//      SeccionDetectada(encabezadoCol, hayDatosEnColumna) por columna.
//      Solo si TODAS las celdas de la primera fila tienen texto (fila de header).
//
//  Si la tabla no encaja en ninguna estructura, contribuye como antes al flag
//  TieneContenido del heading padre (TablaTieneTexto fallback).
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
                    // NIVEL 2: extraer campos/columnas de la tabla.
                    var camposTabla = ExtraerSeccionesTabla(t);
                    resultado.AddRange(camposTabla);

                    // Seguir contribuyendo al TieneContenido del heading padre:
                    // si la tabla no produjo campos estructurados, o si la tabla
                    // tiene contenido (labels con texto cuentan), el heading
                    // queda como "tiene contenido". Esto evita que headings que
                    // solo tienen tablas aparezcan como vacíos en el resumen LLM.
                    if (TablaTieneTexto(t))
                        hayContenidoPendiente = true;
                    break;
            }
        }

        Flush();
        return resultado;
    }

    // -------------------------------------------------------------------------
    //  Extracción de campos a nivel de tabla
    // -------------------------------------------------------------------------

    /// <summary>
    /// Extrae SeccionDetectadas a nivel de campo/columna según la estructura
    /// de la tabla. Devuelve lista vacía si la estructura no es reconocible.
    /// </summary>
    private static IReadOnlyList<SeccionDetectada> ExtraerSeccionesTabla(Table tabla)
    {
        var filas = tabla.Descendants<TableRow>().ToList();
        if (filas.Count == 0) return [];

        var celdasFila0 = filas[0].Descendants<TableCell>().ToList();
        int numCols = celdasFila0.Count;

        if (numCols == 0) return [];

        // Caso A: tabla de 1 columna.
        // Primera fila = label de sección; filas siguientes = contenido.
        if (numCols == 1)
        {
            var label = TextoCelda(celdasFila0[0]).Trim();
            if (string.IsNullOrWhiteSpace(label)) return [];

            bool tieneContenido = filas.Skip(1).Any(fila =>
            {
                var celdas = fila.Descendants<TableCell>().ToList();
                return celdas.Count > 0 && !string.IsNullOrWhiteSpace(TextoCelda(celdas[0]));
            });

            return [new SeccionDetectada(label, tieneContenido)];
        }

        // Caso B: tabla de 2 columnas — pares label/valor.
        // Cada fila donde col[0] tiene texto es un campo.
        if (numCols == 2)
        {
            var secciones = new List<SeccionDetectada>(filas.Count);
            foreach (var fila in filas)
            {
                var celdas = fila.Descendants<TableCell>().ToList();
                if (celdas.Count < 1) continue;
                var label = TextoCelda(celdas[0]).Trim();
                if (string.IsNullOrWhiteSpace(label)) continue;
                var valor = celdas.Count > 1 ? TextoCelda(celdas[1]).Trim() : string.Empty;
                secciones.Add(new SeccionDetectada(label, !string.IsNullOrWhiteSpace(valor)));
            }
            return secciones;
        }

        // Caso C: tabla de N columnas (N≥2) con fila de encabezados.
        // Solo si TODAS las celdas de la primera fila tienen texto.
        var textosFila0 = celdasFila0.Select(c => TextoCelda(c).Trim()).ToList();
        if (textosFila0.All(t => !string.IsNullOrWhiteSpace(t)))
        {
            var secciones = new List<SeccionDetectada>(numCols);
            for (int col = 0; col < numCols; col++)
            {
                var encabezado = textosFila0[col];
                bool tieneContenido = filas.Skip(1).Any(fila =>
                {
                    var celdas = fila.Descendants<TableCell>().ToList();
                    return col < celdas.Count
                        && !string.IsNullOrWhiteSpace(TextoCelda(celdas[col]));
                });
                secciones.Add(new SeccionDetectada(encabezado, tieneContenido));
            }
            return secciones;
        }

        // Estructura no reconocida → sin secciones de campo (fallback al heading).
        return [];
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

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
            foreach (var t in run.Elements<Text>())
                sb.Append(t.Text);
        return sb.ToString().Trim();
    }

    private static string TextoCelda(TableCell cell)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var run in cell.Descendants<Run>())
            foreach (var t in run.Elements<Text>())
                sb.Append(t.Text);
        return sb.ToString();
    }

    private static bool ParrafoTieneTexto(Paragraph p) =>
        TextoDelParrafo(p).Length > 0;

    private static bool TablaTieneTexto(Table table) =>
        table.Descendants<Text>().Any(x => !string.IsNullOrWhiteSpace(x.Text));
}
