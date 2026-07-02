// ============================================================================
//  MetadataScanner — Detección de metadata del encabezado de un formulario
// ----------------------------------------------------------------------------
//  Recibe los bloques de texto que cada parser extrae (header/footer/cuerpo del
//  DOCX, header-footer de impresión y celdas del XLSX, páginas del PDF) y detecta
//  los datos del encabezado común a todos los FR de BDT:
//
//   - Vigencia: etiqueta "Vigencia" + fecha dd/mm/yyyy.
//   - Código:   "FR <n>[-<n>]" (el código del formulario, junto a la vigencia).
//   - Proyecto: etiqueta "Proyecto:" / "Nombre del proyecto:" + su valor.
//
//  Todo es anclado a etiqueta/patrón para no confundirse con otras fechas/textos
//  del documento. Las validaciones (vigencia, código-match, proyecto-match) se
//  construyen sobre lo que detecta este scanner.
// ============================================================================

using System.Text.RegularExpressions;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;

internal static class MetadataScanner
{
    private const string EtiquetaVigencia = "vigencia";

    // Acepta separadores / - . (dd/mm/yyyy, dd-mm-yyyy, dd.mm.yyyy).
    private static readonly Regex FechaRegex = new(
        @"(\d{1,2})[/.\-](\d{1,2})[/.\-](\d{4})",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    // Código del formulario: "FR 48", "FR 48-01", "FR48", "FR 29-05".
    private static readonly Regex CodigoRegex = new(
        @"\bFR\s*\d{1,3}(?:[-\s]\d{1,3})?\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    // Etiqueta "Proyecto:" (o "Nombre del proyecto:") + hasta 80 chars de valor.
    // Exige el ":" para no confundir con títulos ("Tailoring del Proyecto",
    // "Sign Off de Proyecto") que no llevan dos puntos.
    private static readonly Regex ProyectoRegex = new(
        @"(?:nombre\s+del\s+)?proyecto\s*:\s*(.{1,80})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    // ------------------------------------------------------------------------
    //  Vigencia
    // ------------------------------------------------------------------------

    /// <summary>
    /// Devuelve la primera vigencia válida (etiqueta "Vigencia" + fecha) en los
    /// bloques, o null. Anclar a la etiqueta evita confundirla con otras fechas.
    /// </summary>
    public static DateOnly? DetectarVigencia(IEnumerable<string> bloques)
    {
        foreach (var bloque in bloques)
        {
            var v = DetectarVigenciaEnBloque(bloque);
            if (v is not null) return v;
        }
        return null;
    }

    private static DateOnly? DetectarVigenciaEnBloque(string? bloque)
    {
        if (string.IsNullOrWhiteSpace(bloque)) return null;

        var idxEtiqueta = bloque.IndexOf(EtiquetaVigencia, StringComparison.OrdinalIgnoreCase);
        if (idxEtiqueta < 0) return null;

        // 1) Caso normal: la fecha viene DESPUÉS de la etiqueta.
        foreach (Match m in FechaRegex.Matches(bloque))
        {
            if (m.Index < idxEtiqueta) continue;
            if (TryParseFecha(m, out var fecha)) return fecha;
        }

        // 2) Fallback: cualquier fecha del bloque (la etiqueta está presente).
        foreach (Match m in FechaRegex.Matches(bloque))
            if (TryParseFecha(m, out var fecha)) return fecha;

        return null;
    }

    private static bool TryParseFecha(Match m, out DateOnly fecha)
    {
        fecha = default;
        if (!int.TryParse(m.Groups[1].Value, out var dia)) return false;
        if (!int.TryParse(m.Groups[2].Value, out var mes)) return false;
        if (!int.TryParse(m.Groups[3].Value, out var anio)) return false;
        if (mes is < 1 or > 12 || dia is < 1 or > 31) return false;

        try
        {
            fecha = new DateOnly(anio, mes, dia);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false; // ej. 31/02/2024
        }
    }

    // ------------------------------------------------------------------------
    //  Código del formulario
    // ------------------------------------------------------------------------

    /// <summary>
    /// Código "FR &lt;n&gt;[-&lt;n&gt;]" del formulario. Prefiere el que está MÁS
    /// CERCA de la etiqueta "Vigencia": en todos los FR el código y la vigencia van
    /// juntos en el encabezado. Esto evita que en el tailoring —lleno de códigos FR
    /// en la columna "Código Documento Formal" de la tabla— se tome un código de la
    /// tabla en vez del del formulario. Si ningún bloque tiene "Vigencia", cae al
    /// primer "FR &lt;n&gt;" encontrado. Devuelve el texto crudo (ej. "FR 48-01") o null.
    /// </summary>
    public static string? DetectarCodigo(IEnumerable<string> bloques)
    {
        var lista = bloques as IReadOnlyList<string> ?? bloques.ToList();

        // 1) Preferencia: el "FR <n>" más cercano a "Vigencia".
        foreach (var bloque in lista)
        {
            var v = CodigoCercaDeVigencia(bloque);
            if (v is not null) return v;
        }

        // 2) Fallback: primer "FR <n>" en cualquier bloque.
        foreach (var bloque in lista)
        {
            if (string.IsNullOrWhiteSpace(bloque)) continue;
            var m = CodigoRegex.Match(bloque);
            if (m.Success) return m.Value.Trim();
        }

        return null;
    }

    private static string? CodigoCercaDeVigencia(string? bloque)
    {
        if (string.IsNullOrWhiteSpace(bloque)) return null;

        var idxVig = bloque.IndexOf(EtiquetaVigencia, StringComparison.OrdinalIgnoreCase);
        if (idxVig < 0) return null;

        Match? mejor = null;
        int mejorDist = int.MaxValue;
        foreach (Match m in CodigoRegex.Matches(bloque))
        {
            var dist = Math.Abs(m.Index - idxVig);
            if (dist < mejorDist) { mejorDist = dist; mejor = m; }
        }
        return mejor?.Value.Trim();
    }

    // ------------------------------------------------------------------------
    //  Proyecto
    // ------------------------------------------------------------------------

    /// <summary>
    /// Valor del campo "Proyecto:" / "Nombre del proyecto:" (ventana de texto
    /// posterior a la etiqueta). Se usa para proyecto-match con substring, así que
    /// no necesita ser exacto. Devuelve null si no hay un campo así etiquetado.
    /// </summary>
    public static string? DetectarProyecto(IEnumerable<string> bloques)
    {
        foreach (var bloque in bloques)
        {
            if (string.IsNullOrWhiteSpace(bloque)) continue;
            var m = ProyectoRegex.Match(bloque);
            if (m.Success)
            {
                var valor = m.Groups[1].Value.Trim();
                if (valor.Length > 0) return valor;
            }
        }
        return null;
    }
}
