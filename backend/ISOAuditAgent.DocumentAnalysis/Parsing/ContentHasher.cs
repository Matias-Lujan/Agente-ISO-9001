using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ISOAuditAgent.Contracts;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Calcula el <c>HashContenido</c> de <see cref="DocumentoExtraido"/>
/// (SHA-256 hex en minúsculas) según las reglas del contrato:
/// </summary>
/// <list type="bullet">
///   <item><description>
///     <b>Fuentes con archivo binario</b> (<see cref="FuenteDocumento.GoogleDrive"/>):
///     el hash se calcula sobre los <b>bytes originales</b> del archivo
///     descargado. Identifica la versión exacta del binario aunque el
///     parser cambie.
///   </description></item>
///   <item><description>
///     <b>Fuentes sin archivo binario</b> (Trello, Clockify,
///     Microsoft Project): el hash se calcula sobre el <b>texto
///     canónico ya normalizado</b> (UTF-8 / NFC).
///   </description></item>
/// </list>
/// <remarks>
/// Helper estático: no tiene estado y SHA-256 es thread-safe (cada
/// llamada crea una instancia <see cref="IncrementalHash"/> propia).
/// </remarks>
public static class ContentHasher
{
    /// <summary>
    /// Calcula SHA-256 de los bytes del <paramref name="stream"/>,
    /// leyendo desde la posición actual hasta el final, y devuelve el
    /// hash como hex en minúsculas.
    /// </summary>
    /// <remarks>
    /// Si <paramref name="stream"/> es <see cref="Stream.CanSeek"/>,
    /// queda en <c>Position = 0</c> al finalizar para que el caller
    /// pueda re-leerlo (los parsers también lo necesitan).
    /// </remarks>
    public static async Task<string> ComputeSha256OfStreamAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];

        int read;
        while ((read = await stream
            .ReadAsync(buffer.AsMemory(), cancellationToken)
            .ConfigureAwait(false)) > 0)
        {
            sha.AppendData(buffer, 0, read);
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return ToHexLower(sha.GetHashAndReset());
    }

    /// <summary>
    /// Calcula SHA-256 de un buffer de bytes en memoria.
    /// </summary>
    public static string ComputeSha256OfBytes(ReadOnlySpan<byte> bytes)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(bytes, hash);
        return ToHexLower(hash);
    }

    /// <summary>
    /// Calcula SHA-256 del texto codificado como UTF-8.
    /// Si el texto se va a hashear como "contenido canónico" (fuentes
    /// sin binario), se asume que ya pasó por
    /// <see cref="TextNormalizer.Normalize"/>.
    /// </summary>
    public static string ComputeSha256OfText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var maxBytes = Encoding.UTF8.GetMaxByteCount(text.Length);
        var rented = System.Buffers.ArrayPool<byte>.Shared.Rent(maxBytes);
        try
        {
            var written = Encoding.UTF8.GetBytes(text, rented);
            return ComputeSha256OfBytes(rented.AsSpan(0, written));
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static string ToHexLower(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(bytes).ToLower(CultureInfo.InvariantCulture);
}
