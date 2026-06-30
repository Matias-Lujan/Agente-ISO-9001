// ============================================================================
//  VigenciaScanner — Detección de la fecha de Vigencia de un formulario
// ----------------------------------------------------------------------------
//  Recibe bloques de texto ya extraídos por cada parser (header/footer/cuerpo
//  del DOCX, cabecera de la planilla, páginas del PDF) y busca la etiqueta
//  "Vigencia" + la fecha dd/mm/yyyy adyacente.
//
//  POR QUÉ ANCLAR A LA ETIQUETA "Vigencia":
//   Los documentos tienen OTRAS fechas (ej. el historial de cambios del ERS
//   trae fechas en el cuerpo). Tomar "la primera/última fecha del documento"
//   agarraría la equivocada. La vigencia siempre está junto a su etiqueta, en
//   el header (FR 48) o el footer (FR 30).
// ============================================================================

using System.Text.RegularExpressions;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;

internal static class VigenciaScanner
{
    private const string Etiqueta = "vigencia";

    // Acepta separadores / - . (dd/mm/yyyy, dd-mm-yyyy, dd.mm.yyyy) para tolerar
    // variantes de formato al editar el documento a mano.
    private static readonly Regex FechaRegex = new(
        @"(\d{1,2})[/.\-](\d{1,2})[/.\-](\d{4})",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    /// <summary>
    /// Devuelve la primera vigencia válida encontrada en los bloques (en orden),
    /// o null si ninguno tiene la etiqueta "Vigencia" con una fecha.
    /// </summary>
    public static DateOnly? Detectar(IEnumerable<string> bloques)
    {
        foreach (var bloque in bloques)
        {
            var v = DetectarEnBloque(bloque);
            if (v is not null) return v;
        }
        return null;
    }

    private static DateOnly? DetectarEnBloque(string? bloque)
    {
        if (string.IsNullOrWhiteSpace(bloque)) return null;

        var idxEtiqueta = bloque.IndexOf(Etiqueta, StringComparison.OrdinalIgnoreCase);
        if (idxEtiqueta < 0) return null;

        // 1) Caso normal: la fecha viene DESPUÉS de la etiqueta ("Vigencia: dd/mm/yyyy").
        foreach (Match m in FechaRegex.Matches(bloque))
        {
            if (m.Index < idxEtiqueta) continue;
            if (TryParseFecha(m, out var fecha)) return fecha;
        }

        // 2) Fallback: cualquier fecha del bloque (la etiqueta está presente; la
        //    fecha podría quedar antes por el orden de runs/celdas al extraer).
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
}
