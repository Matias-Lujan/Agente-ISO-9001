using System.Text.RegularExpressions;

namespace ISOAuditAgent.API.Mcp.Drive;

/// <summary>
/// Extrae el <c>fileId</c> de enlaces típicos de Google Drive.
/// </summary>
internal static partial class GoogleDriveShareUrl
{
    private static readonly Regex FileDPath = MyRegex();
    private static readonly Regex IdQuery = MyRegex1();

    /// <summary>
    /// Devuelve el id si la URL parece un archivo de Drive; <c>null</c> si no.
    /// </summary>
    public static string? TryExtractFileId(string driveUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveUrl);

        var m = FileDPath.Match(driveUrl);
        if (m.Success)
            return m.Groups[1].Value;

        m = IdQuery.Match(driveUrl);
        if (m.Success)
            return m.Groups[1].Value;

        return null;
    }

    [GeneratedRegex(@"/file/d/([a-zA-Z0-9_-]{10,})", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"[?&]id=([a-zA-Z0-9_-]{10,})", RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex1();
}
