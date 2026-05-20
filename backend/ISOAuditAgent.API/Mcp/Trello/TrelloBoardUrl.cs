using System.Text.RegularExpressions;

namespace ISOAuditAgent.API.Mcp.Trello;

/// <summary>
/// Extrae el identificador de board (id largo o shortLink) de URLs típicas
/// de Trello.
/// </summary>
/// <remarks>
/// Formatos soportados:
/// <list type="bullet">
///   <item><description><c>https://trello.com/b/{shortLink}/{slug}</c></description></item>
///   <item><description><c>https://trello.com/b/{boardId}</c></description></item>
///   <item><description><c>https://trello.com/b/{shortLink}</c></description></item>
/// </list>
/// URLs de tarjeta (<c>/c/...</c>) NO se soportan en MVP — la spec valida
/// boards, no tarjetas individuales.
/// </remarks>
public static partial class TrelloBoardUrl
{
    /// <summary>
    /// Devuelve el id/shortLink si la URL parece un board de Trello;
    /// <c>null</c> si no.
    /// </summary>
    public static string? TryExtractBoardIdOrShortLink(string trelloUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trelloUrl);

        var match = BoardPath().Match(trelloUrl);
        return match.Success ? match.Groups[1].Value : null;
    }

    // /b/ + token alfanumérico de 8+ chars (shortLink suele ser 8; id largo 24 hex).
    [GeneratedRegex(@"/b/([a-zA-Z0-9]{8,})", RegexOptions.CultureInvariant)]
    private static partial Regex BoardPath();
}
