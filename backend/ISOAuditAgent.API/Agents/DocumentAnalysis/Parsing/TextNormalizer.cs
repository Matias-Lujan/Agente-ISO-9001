using System.Globalization;
using System.Text;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Normaliza texto extraído por los parsers para que sea estable y
/// comparable entre invocaciones (Fase 4 — "normalización UTF-8" del
/// entregable). Reglas aplicadas en orden:
/// </summary>
/// <list type="number">
///   <item><description>
///     Si la cadena empieza con BOM (<c>U+FEFF</c>), se descarta.
///   </description></item>
///   <item><description>
///     Line endings unificados a <c>"\n"</c> (descartando <c>\r\n</c> y
///     <c>\r</c> sueltos).
///   </description></item>
///   <item><description>
///     Caracteres de control (categoría Unicode <c>Cc</c>) eliminados,
///     salvo <c>\n</c> y <c>\t</c>.
///   </description></item>
///   <item><description>
///     Normalización Unicode <c>FormC</c> (NFC): los caracteres
///     compuestos (acentos, ñ, etc.) quedan en su forma canónica única
///     — fundamental para multilingüe y para que el SHA-256 del texto
///     sea estable.
///   </description></item>
///   <item><description>
///     <c>TrimEnd</c> de cada línea (elimina espacios al final;
///     conserva indentación).
///   </description></item>
///   <item><description>
///     Colapsa secuencias de 3+ saltos de línea en exactamente 2 (un
///     "párrafo en blanco" como separador máximo).
///   </description></item>
/// </list>
public static class TextNormalizer
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        if (raw[0] == '\uFEFF')
        {
            raw = raw[1..];
        }

        var unifiedEndings = raw.Replace("\r\n", "\n").Replace('\r', '\n');

        var sb = new StringBuilder(unifiedEndings.Length);
        foreach (var ch in unifiedEndings)
        {
            if (ch == '\n' || ch == '\t')
            {
                sb.Append(ch);
                continue;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.Control)
            {
                continue;
            }

            sb.Append(ch);
        }

        var nfc = sb.ToString().Normalize(NormalizationForm.FormC);

        var lines = nfc.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimEnd();
        }

        var joined = string.Join('\n', lines);

        while (joined.Contains("\n\n\n", StringComparison.Ordinal))
        {
            joined = joined.Replace("\n\n\n", "\n\n", StringComparison.Ordinal);
        }

        return joined.Trim('\n');
    }
}
