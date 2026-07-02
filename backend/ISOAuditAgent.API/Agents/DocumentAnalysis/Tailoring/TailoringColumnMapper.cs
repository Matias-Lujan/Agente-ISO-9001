// ============================================================================
//  TailoringColumnMapper — Heurística de mapeo columna → campo (D3.4)
// ----------------------------------------------------------------------------
//  Adaptado del TailoringColumnMapper del diff viejo. Cambios respecto del
//  original:
//   - Agrega aliases para "Artefacto" / "Nombre" — el FR-29 real tiene
//     ambas columnas, y FilaTailoring exige NombreArtefacto no-null.
//   - Quita GetEtapa (no entra al contrato FilaTailoring).
//   - Normalización de headers vive acá (en el original estaba en
//     XlsxTableExtractor, que no rescatamos).
//   - Heurística nueva en GetUrlOrJustificacion: cuando el FR-29 mezcla
//     URL y justificación en la misma columna, detectamos por prefijo
//     http/https o por presencia de "drive.google.com".
// ============================================================================

using System.Globalization;
using System.Text;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;

internal static class TailoringColumnMapper
{
    // Grupos de aliases. Coincidencia case-insensitive, sin tildes.
    // Aliases largos permiten subcadena; aliases ≤3 letras solo igualdad.

    private static readonly string[] CodigoAliases =
    {
        "codigo documento formal", "codigo artefacto",
        "codigo", "referencia", "fr"
    };

    private static readonly string[] NombreAliases =
    {
        "artefacto", "nombre artefacto", "nombre", "documento"
    };

    private static readonly string[] AplicaAliases =
    {
        "aplica en proyecto", "aplica", "incluye", "requiere", "activo"
    };

    private static readonly string[] JustificacionUrlAliases =
    {
        // El header real del FR-29 es largo — usamos subcadenas tolerantes.
        "justificacion o ubicacion del artefacto generado",
        "justificacion o ubicacion",
        "justificacion", "motivo", "comentario", "observacion",
        "ubicacion", "url", "link", "enlace", "drive", "ruta"
    };

    public static string? GetCodigo(IReadOnlyDictionary<string, string> row) =>
        Find(row, CodigoAliases);

    public static string? GetNombre(IReadOnlyDictionary<string, string> row) =>
        Find(row, NombreAliases);

    public static string? GetAplicaRaw(IReadOnlyDictionary<string, string> row) =>
        Find(row, AplicaAliases);

    /// <summary>
    /// Devuelve (justificacion, url) tras aplicar la heurística:
    /// celda con prefijo http://, https:// o que contiene drive.google.com
    /// va a URL; cualquier otro texto va a justificación.
    /// </summary>
    public static (string? Justificacion, string? Url) GetJustificacionYUrl(
        IReadOnlyDictionary<string, string> row)
    {
        var celda = Find(row, JustificacionUrlAliases);
        if (string.IsNullOrWhiteSpace(celda))
            return (null, null);

        var trimmed = celda.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("drive.google.com", StringComparison.OrdinalIgnoreCase))
        {
            return (null, trimmed);
        }

        return (trimmed, null);
    }

    /// <summary>
    /// Interpreta la celda "aplica" en forma laxa. Maneja:
    ///  - "Si", "Sí", "S", "Y", "Yes", "X", "OK", "true", "1"  → true
    ///  - "No", "N", "false", "0"                              → false
    ///  - "No (justificar)" — se reconoce porque contiene "no" → false
    ///  - "n/a", "-", vacío                                    → null
    /// </summary>
    public static bool? ParseAplica(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var t = raw.Trim();
        var lower = t.ToLowerInvariant();

        if (bool.TryParse(t, out var b)) return b;

        if (int.TryParse(t, out var n))
        {
            if (n == 1) return true;
            if (n == 0) return false;
        }

        // Caso especial del FR-29 real: "No (justificar)".
        // Lo detectamos porque empieza con "no" (la frase entera contiene "no"
        // como token inicial). Match conservador para no confundir "nominal".
        if (lower.StartsWith("no ", StringComparison.Ordinal)
            || lower == "no" || lower == "n")
        {
            return false;
        }

        return lower switch
        {
            "si" or "sí" or "s" or "y" or "yes" or "x" or "ok" => true,
            "false" => false,
            "n/a" or "na" or "-" => null,
            _ => null
        };
    }

    /// <summary>
    /// Indica si los encabezados tienen al menos una columna reconocible para
    /// código O nombre de artefacto. Sin al menos una de las dos no se puede
    /// construir FilaTailoring.
    /// </summary>
    public static bool HeadersTienenCodigoONombre(IReadOnlyList<string> headers)
    {
        foreach (var h in headers)
        {
            var n = NormalizeHeaderKey(h);
            if (string.IsNullOrEmpty(n)) continue;
            if (MatchesAnyAlias(n, CodigoAliases)) return true;
            if (MatchesAnyAlias(n, NombreAliases)) return true;
        }
        return false;
    }

    /// <summary>
    /// Normaliza un header: lowercase, sin tildes, whitespace colapsado.
    /// Internal porque TailoringReader lo usa al construir las filas.
    /// </summary>
    public static string NormalizeHeaderKey(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        // Lowercase invariante.
        var lower = raw.ToLowerInvariant();

        // Remover tildes vía descomposición Unicode.
        var sb = new StringBuilder(lower.Length);
        foreach (var c in lower.Normalize(NormalizationForm.FormD))
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        // Colapsar whitespace: cualquier secuencia → un solo espacio.
        var s = sb.ToString().Trim();
        var resultado = new StringBuilder(s.Length);
        var lastWasSpace = false;
        foreach (var c in s)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace) resultado.Append(' ');
                lastWasSpace = true;
            }
            else
            {
                resultado.Append(c);
                lastWasSpace = false;
            }
        }

        return resultado.ToString();
    }

    private static string? Find(
        IReadOnlyDictionary<string, string> row, string[] aliases)
    {
        foreach (var kv in row)
        {
            if (!MatchesAnyAlias(kv.Key, aliases)) continue;
            var v = kv.Value.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }
        return null;
    }

    private static bool MatchesAnyAlias(string normalizedKey, string[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (normalizedKey == alias) return true;
            if (alias.Length <= 3) continue;
            if (normalizedKey.Contains(alias, StringComparison.Ordinal)) return true;
        }
        return false;
    }
}