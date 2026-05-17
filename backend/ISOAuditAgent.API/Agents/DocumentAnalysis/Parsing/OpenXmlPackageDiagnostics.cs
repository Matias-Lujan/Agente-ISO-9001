using System.Globalization;
using System.Text;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Helpers para validar y diagnosticar paquetes OpenXml (DOCX/XLSX) antes
/// de delegar al SDK de <c>DocumentFormat.OpenXml</c>. Expone dos puntos de
/// uso:
/// </summary>
/// <remarks>
/// <list type="number">
///   <item><description>
///     <see cref="ValidateLooksLikeOpenXml"/>: pre-check barato (lee 4
///     bytes) que falla temprano con <see cref="InvalidDataException"/> y
///     un mensaje accionable cuando el contenido no es un paquete
///     OpenXml plausible (vacío, demasiado pequeño, sin firma ZIP).
///   </description></item>
///   <item><description>
///     <see cref="DescribeFailure"/>: decora cualquier excepción
///     proveniente del SDK con tamaño, primeros bytes (hex + ASCII),
///     <i>magic number</i> detectado y causas habituales. Se invoca desde
///     el catch del parser para producir un mensaje útil que el endpoint
///     dev-only / MCP / runner puedan exponer al cliente.
///   </description></item>
/// </list>
/// </remarks>
internal static class OpenXmlPackageDiagnostics
{
    /// <summary>
    /// Tamaño mínimo razonable para un paquete OpenXml. Un DOCX/XLSX
    /// "vacío" generado por Office o por el propio SDK pesa varios KB
    /// (al menos <c>[Content_Types].xml</c>, <c>_rels/.rels</c> y la
    /// parte principal). Por debajo de este umbral asumimos truncado.
    /// </summary>
    private const int MinReasonableOpenXmlSize = 200;

    private const int HeadBytesToInspect = 16;

    public static void ValidateLooksLikeOpenXml(Stream seekable, string nombreArchivo)
    {
        if (!seekable.CanSeek)
        {
            return;
        }

        var length = seekable.Length;
        var posOriginal = seekable.Position;
        seekable.Position = 0;

        try
        {
            if (length == 0)
            {
                throw new InvalidDataException(
                    $"El archivo '{nombreArchivo}' está vacío (0 bytes).");
            }

            if (length < MinReasonableOpenXmlSize)
            {
                throw new InvalidDataException(
                    $"El archivo '{nombreArchivo}' es demasiado pequeño " +
                    $"({length} bytes) para ser un paquete OpenXml plausible; " +
                    "probablemente esté truncado o corrupto en la fuente.");
            }

            Span<byte> head = stackalloc byte[4];
            var read = seekable.Read(head);
            if (read < 4 || !IsZipSignature(head))
            {
                throw new InvalidDataException(
                    $"El archivo '{nombreArchivo}' no comienza con la firma ZIP " +
                    "esperada (\"PK..\"); no es un paquete OpenXml válido.");
            }
        }
        finally
        {
            seekable.Position = posOriginal;
        }
    }

    public static string DescribeFailure(
        Exception ex,
        Stream? contenido,
        string nombreArchivo,
        string tipoEsperado)
    {
        var sb = new StringBuilder();
        sb.Append(ex.Message);

        if (contenido is { CanSeek: true })
        {
            sb.Append(" (archivo='").Append(nombreArchivo).Append('\'');
            sb.Append(", tamaño=").Append(contenido.Length).Append(" bytes");

            var primerosBytes = TryReadHead(contenido, HeadBytesToInspect);
            if (primerosBytes is not null && primerosBytes.Length > 0)
            {
                sb.Append(", primeros bytes: ").Append(FormatHead(primerosBytes));
                sb.Append(", magic detectado: ").Append(GuessFileTypeFromMagic(primerosBytes));
            }
            sb.Append(')');
        }
        else
        {
            sb.Append(" (archivo='").Append(nombreArchivo).Append("')");
        }

        sb.Append(". Tipo esperado: ").Append(tipoEsperado).Append('.');
        sb.Append(" Causas habituales: archivo corrupto o truncado en la fuente, ");
        sb.Append("exportación incompleta desde Google Docs, atajo (shortcut) de Drive ");
        sb.Append("o archivo con extensión/MIME equivocados.");

        return sb.ToString();
    }

    private static byte[]? TryReadHead(Stream contenido, int max)
    {
        try
        {
            var posOriginal = contenido.Position;
            contenido.Position = 0;
            var len = (int)Math.Min(max, contenido.Length);
            if (len <= 0) return Array.Empty<byte>();

            var bytes = new byte[len];
            var read = contenido.Read(bytes, 0, bytes.Length);
            contenido.Position = posOriginal;
            return read == bytes.Length ? bytes : bytes[..read];
        }
        catch
        {
            return null;
        }
    }

    private static string FormatHead(byte[] head)
    {
        var hex = new StringBuilder(head.Length * 3);
        var ascii = new StringBuilder(head.Length);

        foreach (var b in head)
        {
            hex.Append(b.ToString("X2", CultureInfo.InvariantCulture)).Append(' ');
            ascii.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
        }

        if (hex.Length > 0) hex.Length--;

        return $"hex=[{hex}] ascii=\"{ascii}\"";
    }

    private static bool IsZipSignature(ReadOnlySpan<byte> head)
    {
        if (head.Length < 4) return false;
        if (head[0] != 0x50 || head[1] != 0x4B) return false;

        return head[2] switch
        {
            0x03 => head[3] == 0x04,
            0x05 => head[3] == 0x06,
            0x07 => head[3] == 0x08,
            _ => false
        };
    }

    private static string GuessFileTypeFromMagic(byte[] head)
    {
        if (head.Length >= 4
            && head[0] == 0x50 && head[1] == 0x4B
            && (head[2] == 0x03 || head[2] == 0x05 || head[2] == 0x07))
        {
            return "ZIP/OpenXml";
        }

        if (head.Length >= 4
            && head[0] == 0x25 && head[1] == 0x50 && head[2] == 0x44 && head[3] == 0x46)
        {
            return "PDF";
        }

        if (head.Length >= 8
            && head[0] == 0xD0 && head[1] == 0xCF && head[2] == 0x11 && head[3] == 0xE0
            && head[4] == 0xA1 && head[5] == 0xB1 && head[6] == 0x1A && head[7] == 0xE1)
        {
            return "OLE2 (.doc/.xls legado)";
        }

        var asAscii = SafeAscii(head);

        if (asAscii.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            return "XML";
        }

        if (asAscii.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || asAscii.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            return "HTML (¿página de error de Drive?)";
        }

        if (asAscii.StartsWith("{") || asAscii.StartsWith("["))
        {
            return "JSON";
        }

        return "desconocido";
    }

    private static string SafeAscii(byte[] head)
    {
        var len = Math.Min(head.Length, 16);
        var chars = new char[len];
        for (var i = 0; i < len; i++)
        {
            var b = head[i];
            chars[i] = b is >= 0x20 and < 0x7F ? (char)b : '.';
        }
        return new string(chars);
    }
}
