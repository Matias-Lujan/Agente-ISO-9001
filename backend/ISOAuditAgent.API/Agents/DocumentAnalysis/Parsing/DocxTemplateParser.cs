// ============================================================================
//  DocxTemplateParser — Parser de .docx con DocumentFormat.OpenXml
// ----------------------------------------------------------------------------
//  PROBLEMA QUE RESUELVE (adaptabilidad multi-procedimiento):
//
//  Los documentos reales de BDT — tanto los nativos de Office como los
//  exportados desde Google Docs — NO usan estilos semánticos de heading
//  (Heading1, outlineLvl). Los títulos de sección son párrafos sin estilo, y
//  el formato visual que los distingue es INCONSISTENTE: en el ERS (FR-30) el
//  título NO es negrita y las columnas de tabla SÍ; el tamaño de fuente es el
//  único signal visual, pero depende del docDefaults de cada archivo. Apoyarse
//  en negrita o en un umbral fijo de tamaño no generaliza a procedimientos
//  futuros.
//
//  ESTRATEGIA: detección ESTRUCTURAL (confiable y agnóstica al procedimiento),
//  no visual (frágil). Las tablas están bien estructuradas en OOXML y se
//  parsean igual en Office y en exportaciones de Google. Tres fuentes de
//  sección, en orden de prioridad:
//
//   1. Heading semántico: párrafo con OutlineLevel o ParagraphStyleId de
//      heading (EN/ES). Es lo correcto cuando el documento lo usa; se mantiene
//      para documentos bien formados (futuro).
//
//   2. Título de tarjeta: una tabla cuya PRIMERA FILA es una sola celda
//      (combinada) con texto. Ese texto es el título de la sección y el resto
//      de la tabla es su contenido. Patrón del Sign-Off (FR-48): cada sección
//      es una tarjeta ("Resumen", "Entregables", "Aceptación...").
//
//   3. Caption de tabla: el párrafo no vacío inmediatamente anterior a una
//      tabla la nombra, SIN importar su formato. Patrón del ERS (FR-30):
//      "Documentos relacionados" (párrafo) seguido de una tabla de columnas
//      Documento/Descripción/Autor. Así el hallazgo es "falta Documentos
//      relacionados" (sección real) y no "falta Documento" (una columna).
//
//  CAMPOS CALIFICADOS POR SU SECCIÓN PADRE:
//   Los campos/columnas de una tabla se emiten como "Sección / Campo"
//   (ej. "Documentos relacionados / Documento"). Esto preserva la detección de
//   campos vacíos (ej. "Historial de Cambios / Autor" vacío) sin confundir un
//   campo con una sección de primer nivel. El matching documento↔template usa
//   NormalizeHeaderKey, que conserva el "/", así que template y documento
//   (parseados con la misma lógica) machean.
//
//  LIMITACIÓN ACEPTADA: las secciones de SOLO TEXTO sin tabla asociada
//  (ej. "Propósito del documento" seguido de un párrafo) no se detectan como
//  sección propia, porque no hay signal estructural confiable y el tamaño de
//  fuente es demasiado frágil para garantizar adaptabilidad. El fix robusto de
//  largo plazo es que los templates usen estilos de heading reales (fuera de
//  nuestro control). Mientras tanto se prioriza NO generar falsos positivos.
// ============================================================================

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;

public sealed class DocxTemplateParser : ITemplateParser
{
    /// <summary>
    /// Separador entre una sección y sus campos: "Sección › Campo".
    /// Se usa "›" (U+203A) deliberadamente: NO aparece en el texto de los
    /// documentos de BDT, a diferencia de "/" que sí existe dentro de nombres de
    /// columna reales (ej. la columna "Descripcion Tarea / Riesgo" del FR-31).
    /// Un separador que colisiona con contenido real haría que la supresión de
    /// campos huérfanos en HallazgosEstructurales descarte hallazgos legítimos.
    /// </summary>
    public const string SeparadorCampo = " › ";

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

    /// <summary>
    /// La vigencia vive en el header (ej. FR 48) o el footer (ej. FR 30), junto al
    /// código del formulario; algunos documentos la ponen en el cuerpo. Se devuelven
    /// los tres orígenes como bloques (headers y footers primero, cuerpo al final).
    /// </summary>
    public IReadOnlyList<string> ExtraerBloquesMetadata(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length == 0) return Array.Empty<string>();

        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            using var doc = WordprocessingDocument.Open(ms, isEditable: false);

            var main = doc.MainDocumentPart;
            if (main is null) return Array.Empty<string>();

            var bloques = new List<string>();
            foreach (var h in main.HeaderParts) bloques.Add(TextoDeElemento(h.Header));
            foreach (var f in main.FooterParts) bloques.Add(TextoDeElemento(f.Footer));
            bloques.Add(TextoDeElemento(main.Document?.Body));
            return bloques;
        }
        catch
        {
            // Robusto: la vigencia es accesoria; si el paquete falla devolvemos vacío.
            return Array.Empty<string>();
        }
    }

    private static string TextoDeElemento(DocumentFormat.OpenXml.OpenXmlElement? root)
    {
        if (root is null) return string.Empty;
        var sb = new System.Text.StringBuilder();
        // Concatenar los runs DENTRO de cada párrafo SIN separador: un run es un
        // corte arbitrario de texto y Word parte un token (ej. una fecha) en varios
        // runs al editarlo a mano. Meter un espacio entre runs rompería
        // "01/07/2021" → "01/07/ 2021" y la regex no la reconocería. Los párrafos y
        // celdas SÍ se separan con espacio, para no fusionar bloques distintos (ej.
        // la etiqueta "Vigencia" y la fecha en celdas vecinas del header).
        foreach (var parrafo in root.Descendants<Paragraph>())
        {
            foreach (var t in parrafo.Descendants<Text>())
                sb.Append(t.Text);
            sb.Append(' ');
        }
        return sb.ToString();
    }

    private static IReadOnlyList<SeccionDetectada> ExtraerSecciones(Body body)
    {
        var resultado = new List<SeccionDetectada>();

        string? headingActual = null;
        var headingTieneContenido = false;
        string? captionPendiente = null;   // último párrafo no vacío (caption de tabla)

        void FlushHeading()
        {
            if (headingActual is not null)
                resultado.Add(new SeccionDetectada(headingActual, headingTieneContenido));
        }

        foreach (var child in body.ChildElements)
        {
            switch (child)
            {
                case Paragraph p:
                    if (EsHeading(p))
                    {
                        FlushHeading();
                        headingActual = TextoDelParrafo(p);
                        headingTieneContenido = false;
                        captionPendiente = headingActual;
                    }
                    else if (ParrafoTieneTexto(p))
                    {
                        headingTieneContenido = true;
                        captionPendiente = TextoDelParrafo(p);
                    }
                    // Párrafo vacío: no toca el caption pendiente (es común que haya
                    // párrafos vacíos entre el título y la tabla).
                    break;

                case Table t:
                    var (cardTitle, campos, tablaTieneDatos) = AnalizarTabla(t);
                    var seccionNombre = cardTitle ?? captionPendiente;

                    if (seccionNombre is not null)
                    {
                        var seccionTieneContenido =
                            tablaTieneDatos || campos.Any(c => c.TieneValor);

                        // Si el caption ES el heading semántico abierto, no duplicar
                        // la sección: solo marcar que tiene contenido.
                        var esElHeadingAbierto = headingActual is not null
                            && Clave(seccionNombre) == Clave(headingActual);

                        if (esElHeadingAbierto)
                        {
                            headingTieneContenido = headingTieneContenido || seccionTieneContenido;
                        }
                        else
                        {
                            resultado.Add(new SeccionDetectada(seccionNombre, seccionTieneContenido));
                        }

                        foreach (var c in campos)
                        {
                            resultado.Add(new SeccionDetectada(
                                $"{seccionNombre}{SeparadorCampo}{c.Label}", c.TieneValor));
                        }
                    }
                    else
                    {
                        // Sin nombre de sección (tabla sin caption ni título de
                        // tarjeta): emitir campos sueltos (comportamiento de fallback).
                        foreach (var c in campos)
                            resultado.Add(new SeccionDetectada(c.Label, c.TieneValor));
                    }

                    captionPendiente = null; // consumido por la tabla
                    break;
            }
        }

        FlushHeading();

        return DedupPreservandoContenido(resultado);
    }

    // -------------------------------------------------------------------------
    //  Análisis de tabla
    // -------------------------------------------------------------------------

    private readonly record struct Campo(string Label, bool TieneValor);

    /// <summary>
    /// Analiza una tabla y devuelve:
    ///   - CardTitle: título de la sección si la tabla es una "tarjeta" (fila 0 =
    ///     una sola celda con texto). null si no.
    ///   - Campos: campos/columnas de la tabla (vacío para tarjetas).
    ///   - TieneDatos: si hay texto debajo de la fila de encabezado/título.
    /// </summary>
    private static (string? CardTitle, List<Campo> Campos, bool TieneDatos) AnalizarTabla(Table tabla)
    {
        var filas = tabla.Descendants<TableRow>().ToList();
        if (filas.Count == 0) return (null, new List<Campo>(), false);

        var celdasFila0 = filas[0].Descendants<TableCell>().ToList();
        int numCols = celdasFila0.Count;
        if (numCols == 0) return (null, new List<Campo>(), false);

        bool HayTextoEnFilas(IEnumerable<TableRow> rows) =>
            rows.Any(f => f.Descendants<TableCell>()
                .Any(c => !string.IsNullOrWhiteSpace(TextoCelda(c))));

        // Tarjeta: fila 0 es UNA sola celda (combinada) con texto → es el título
        // de la sección; el resto de la tabla es su contenido.
        if (numCols == 1)
        {
            var titulo = TextoCelda(celdasFila0[0]).Trim();
            if (string.IsNullOrWhiteSpace(titulo))
                return (null, new List<Campo>(), false);

            var contenido = HayTextoEnFilas(filas.Skip(1));
            return (titulo, new List<Campo>(), contenido);
        }

        // Tabla regular: la nombra el caller (caption). Extraer campos.
        var campos = ExtraerCamposRegulares(filas, celdasFila0, numCols);
        var tieneDatos = HayTextoEnFilas(filas.Skip(1));
        return (null, campos, tieneDatos);
    }

    /// <summary>
    /// Extrae campos de una tabla regular (≥2 columnas):
    ///   - 2 columnas: cada fila es un par label/valor.
    ///   - ≥3 columnas con fila 0 toda llena: tabla de datos → columnas como campos.
    /// </summary>
    private static List<Campo> ExtraerCamposRegulares(
        List<TableRow> filas, List<TableCell> celdasFila0, int numCols)
    {
        // 2 columnas: pares label/valor.
        if (numCols == 2)
        {
            var campos = new List<Campo>(filas.Count);
            foreach (var fila in filas)
            {
                var celdas = fila.Descendants<TableCell>().ToList();
                if (celdas.Count < 1) continue;
                var label = TextoCelda(celdas[0]).Trim();
                if (string.IsNullOrWhiteSpace(label)) continue;
                var valor = celdas.Count > 1 ? TextoCelda(celdas[1]).Trim() : string.Empty;
                campos.Add(new Campo(label, !string.IsNullOrWhiteSpace(valor)));
            }
            return campos;
        }

        // ≥3 columnas con fila 0 de encabezados (todas las celdas con texto).
        var textosFila0 = celdasFila0.Select(c => TextoCelda(c).Trim()).ToList();
        if (textosFila0.All(t => !string.IsNullOrWhiteSpace(t)))
        {
            var campos = new List<Campo>(numCols);
            for (int col = 0; col < numCols; col++)
            {
                var encabezado = textosFila0[col];
                bool tieneContenido = filas.Skip(1).Any(fila =>
                {
                    var celdas = fila.Descendants<TableCell>().ToList();
                    return col < celdas.Count
                        && !string.IsNullOrWhiteSpace(TextoCelda(celdas[col]));
                });
                campos.Add(new Campo(encabezado, tieneContenido));
            }
            return campos;
        }

        // Estructura no reconocida → sin campos.
        return new List<Campo>();
    }

    /// <summary>
    /// Dedup por clave normalizada. Si una sección aparece varias veces (ej. el
    /// parser la ve como heading y como tabla), se conserva una sola, marcándola
    /// con contenido si CUALQUIER aparición tenía contenido. Evita que una sección
    /// con contenido quede registrada como vacía.
    /// </summary>
    private static IReadOnlyList<SeccionDetectada> DedupPreservandoContenido(
        List<SeccionDetectada> secciones)
    {
        var orden = new List<string>();
        var porClave = new Dictionary<string, SeccionDetectada>();

        foreach (var s in secciones)
        {
            var clave = Clave(s.Titulo);
            if (porClave.TryGetValue(clave, out var existente))
            {
                if (s.TieneContenido && !existente.TieneContenido)
                    porClave[clave] = existente with { TieneContenido = true };
            }
            else
            {
                porClave[clave] = s;
                orden.Add(clave);
            }
        }

        return orden.Select(k => porClave[k]).ToList();
    }

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    private static string Clave(string s) => TailoringColumnMapper.NormalizeHeaderKey(s);

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
}
