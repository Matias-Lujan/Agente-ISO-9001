using ISOAuditAgent.DocumentAnalysis.Parsing;

namespace ISOAuditAgent.DocumentAnalysis.Tailoring;

/// <summary>
/// Resuelve columnas de planilla por alias tolerantes (mayúsculas, tildes
/// en encabezados comunes, reordenamiento de columnas).
/// </summary>
internal static class TailoringColumnMapper
{
    private static readonly string[][] CodigoAliases =
    {
        new[]
        {
            "codigo artefacto", "codigo", "código", "código artefacto",
            "referencia", "item"
        },
        new[] { "fr" },
        new[] { "id" }
    };

    private static readonly string[][] EtapaAliases =
    {
        new[] { "etapa", "fase", "etapa procedimiento", "etapa del procedimiento" }
    };

    private static readonly string[][] AplicaAliases =
    {
        new[] { "aplica", "aplica en proyecto", "incluye", "requiere", "activo" }
    };

    private static readonly string[][] JustificacionAliases =
    {
        new[]
        {
            "justificacion", "justificación", "motivo", "comentario",
            "observacion", "observación", "razon", "razón"
        }
    };

    private static readonly string[][] UrlAliases =
    {
        new[] { "url", "link", "enlace", "drive", "ubicacion", "ubicación", "ruta", "path" }
    };

    public static string? GetCodigo(IReadOnlyDictionary<string, string> row) =>
        Find(row, CodigoAliases);

    public static string? GetEtapa(IReadOnlyDictionary<string, string> row) =>
        Find(row, EtapaAliases);

    public static string? GetAplicaRaw(IReadOnlyDictionary<string, string> row) =>
        Find(row, AplicaAliases);

    public static string? GetJustificacion(IReadOnlyDictionary<string, string> row) =>
        Find(row, JustificacionAliases);

    public static string? GetUrl(IReadOnlyDictionary<string, string> row) =>
        Find(row, UrlAliases);

    /// <summary>
    /// Indica si al menos un encabezado parece la columna de código de artefacto.
    /// </summary>
    public static bool HeadersSuggestCodigoColumn(IReadOnlyList<string> headers)
    {
        foreach (var h in headers)
        {
            var n = XlsxTableExtractor.NormalizeHeaderKey(h);
            if (string.IsNullOrEmpty(n)) continue;
            if (MatchesAnyAlias(n, CodigoAliases)) return true;
        }

        return false;
    }

    private static string? Find(
        IReadOnlyDictionary<string, string> row,
        IReadOnlyList<string[]> aliasGroups)
    {
        foreach (var kv in row)
        {
            var keyNorm = XlsxTableExtractor.NormalizeHeaderKey(kv.Key);
            if (!MatchesAnyAlias(keyNorm, aliasGroups)) continue;

            var v = kv.Value.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }

        return null;
    }

    private static bool MatchesAnyAlias(string normalizedKey, IReadOnlyList<string[]> aliasGroups)
    {
        foreach (var group in aliasGroups)
        {
            foreach (var alias in group)
            {
                if (AliasMatches(normalizedKey, alias))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Aliases largos permiten subcadena; los cortes (≤3 letras) solo igualdad.
    /// </summary>
    private static bool AliasMatches(string normalizedKey, string alias)
    {
        if (normalizedKey == alias) return true;
        if (alias.Length <= 3) return false;
        if (normalizedKey.Contains(alias, StringComparison.Ordinal)) return true;
        return normalizedKey.StartsWith(alias + ' ', StringComparison.Ordinal);
    }

    /// <summary>
    /// Interpreta la celda “aplica” en forma laxa (es/ en / bool / 1 / 0).
    /// </summary>
    public static bool? ParseAplica(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var t = raw.Trim();

        if (bool.TryParse(t, out var b)) return b;

        if (int.TryParse(t, out var n))
        {
            if (n == 1) return true;
            if (n == 0) return false;
        }

        var lower = t.ToLowerInvariant();
        return lower switch
        {
            "si" or "sí" or "s" or "y" or "yes" or "x" or "ok" => true,
            "no" or "n" or "false" => false,
            _ when lower is "n/a" or "na" or "-" => null,
            _ => null
        };
    }
}
