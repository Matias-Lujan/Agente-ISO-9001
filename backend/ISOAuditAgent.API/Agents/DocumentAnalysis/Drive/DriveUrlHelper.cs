// ============================================================================
//  DriveUrlHelper — Extracción de fileId desde URLs de Google Drive (D3.5)
// ----------------------------------------------------------------------------
//  Pure, sin estado. Reconoce los 3 formatos típicos que el usuario pega en
//  el FR-29:
//
//   https://drive.google.com/file/d/{ID}/view?usp=sharing       (más común)
//   https://drive.google.com/file/d/{ID}/edit
//   https://docs.google.com/document/d/{ID}/edit                (Google Docs)
//   https://docs.google.com/spreadsheets/d/{ID}/edit            (Sheets)
//   https://drive.google.com/open?id={ID}                       (formato viejo)
//
//  Devuelve null si no logra extraer un fileId. El caller (ArtefactoFisico-
//  Checker) hace fallback a búsqueda por código/nombre cuando esto pasa.
// ============================================================================

using System.Text.RegularExpressions;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Drive;

internal static class DriveUrlHelper
{
    // /d/{id} captura "/file/d/<id>" y "/document/d/<id>" y "/spreadsheets/d/<id>".
    private static readonly Regex SlashD = new(
        @"/d/([a-zA-Z0-9_-]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    // ?id=<id> o &id=<id> para el formato open?id=...
    private static readonly Regex QueryId = new(
        @"[?&]id=([a-zA-Z0-9_-]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    /// <summary>
    /// Intenta extraer el fileId de una URL de Drive. Devuelve null si la URL
    /// es null, vacía, no es de Drive, o no tiene un patrón reconocible.
    /// </summary>
    public static string? TryExtractFileId(string? driveUrl)
    {
        if (string.IsNullOrWhiteSpace(driveUrl)) return null;

        var url = driveUrl.Trim();

        var m1 = SlashD.Match(url);
        if (m1.Success) return m1.Groups[1].Value;

        var m2 = QueryId.Match(url);
        if (m2.Success) return m2.Groups[1].Value;

        return null;
    }
}