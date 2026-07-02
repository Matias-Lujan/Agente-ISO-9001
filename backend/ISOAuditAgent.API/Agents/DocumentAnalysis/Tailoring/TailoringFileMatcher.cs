// ============================================================================
//  TailoringFileMatcher — Puntuación de candidatos a archivo de tailoring
// ----------------------------------------------------------------------------
//  Dado el código y el nombre del artefacto de tailoring (que vienen del MARCO
//  en la BD — ArtefactoEsperado.EsTailoring —, NO hardcodeados), puntúa qué tan
//  probable es que un archivo del folder de Drive sea ese tailoring:
//    +10  el código del artefacto aparece en el nombre del archivo (tolerante a
//         espacios/guiones/puntos: "FR 29" ≈ "FR29" ≈ "FR-29").
//    +5   una palabra significativa del nombre del artefacto ("Tailoring")
//         aparece en el nombre del archivo.
//
//  Lógica pura (sin I/O) para poder testearla con strings.
// ============================================================================

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;

internal static class TailoringFileMatcher
{
    public static int Score(string fileName, string? tailoringCodigo, string tailoringNombre)
    {
        var name = fileName?.Trim() ?? string.Empty;
        if (name.Length == 0) return 0;

        var score = 0;

        var cod = NormalizarCodigo(tailoringCodigo);
        if (cod.Length > 0 && NormalizarCodigo(name).Contains(cod, StringComparison.Ordinal))
            score += 10;

        var keyword = PrimerTokenSignificativo(tailoringNombre);
        if (keyword is not null && name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            score += 5;

        return score;
    }

    /// <summary>
    /// True si el nombre del archivo contiene el código del tailoring (tolerante a
    /// espacios/guiones/puntos). false si el código es null/vacío. Se usa para EXIGIR
    /// el código: un archivo sin ese código en el nombre NO es el tailoring, aunque
    /// el nombre coincida (mismo criterio que ArtefactoFisicoChecker con los demás FR).
    /// </summary>
    public static bool CoincideCodigo(string fileName, string? tailoringCodigo)
    {
        var cod = NormalizarCodigo(tailoringCodigo);
        if (cod.Length == 0) return false;
        return NormalizarCodigo(fileName ?? string.Empty).Contains(cod, StringComparison.Ordinal);
    }

    // "FR 29" / "FR-29" / "fr.29" → "fr29". Mismo criterio que ArtefactoFisicoChecker.
    private static string NormalizarCodigo(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (char.IsWhiteSpace(c) || c is '-' or '_' or '.') continue;
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    // Primera palabra "distintiva" (> 3 chars) del nombre del artefacto. Para
    // "Tailoring del Proyecto" → "tailoring". null si no hay ninguna.
    private static string? PrimerTokenSignificativo(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return null;

        foreach (var token in nombre.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            if (token.Length > 3) return token.ToLowerInvariant();

        return null;
    }
}
