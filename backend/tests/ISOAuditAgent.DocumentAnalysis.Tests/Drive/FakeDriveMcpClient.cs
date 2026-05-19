using System.Text.RegularExpressions;
using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Mcp.Drive;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Drive;

/// <summary>
/// Implementación en memoria de <see cref="IDriveMcpClient"/> para tests:
/// reutiliza <see cref="DriveListingService"/> + <see cref="IDriveClient"/>
/// igual que el servidor MCP, sin HTTP.
/// </summary>
internal sealed partial class FakeDriveMcpClient : IDriveMcpClient
{
    private readonly IProyectoDriveResolver _resolver;
    private readonly IDriveListingService _listing;
    private readonly IDriveClient _client;

    public FakeDriveMcpClient(IOptions<DocumentAnalysisOptions> options, IDriveClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        _resolver = new OptionsBackedProyectoDriveResolver(options);
        var policy = new DriveExclusionPolicy(options);
        _listing = new DriveListingService(_resolver, client, policy, options);
        _client = client;
    }

    public async Task<DriveFileListing> ListProjectFilesAsync(
        int proyectoId,
        CancellationToken cancellationToken = default)
    {
        var folderId = _resolver.ResolveFolderId(proyectoId);
        var files = new List<DriveFile>();
        await foreach (var file in _listing
                           .ListFilesUnderProjectAsync(proyectoId, cancellationToken)
                           .ConfigureAwait(false))
        {
            files.Add(file);
        }

        return new DriveFileListing(proyectoId, folderId, files);
    }

    public Task<DriveFileContent> GetFileContentAsync(
        string fileId,
        CancellationToken cancellationToken = default) =>
        _client.DownloadFileAsync(fileId, cancellationToken);

    public Task<DriveFileContent> GetFileContentByDriveUrlAsync(
        string driveUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveUrl);
        var fileId = TryExtractFileId(driveUrl.Trim());
        if (string.IsNullOrEmpty(fileId))
        {
            throw new InvalidOperationException(
                "La URL no contiene un identificador de archivo de Drive reconocible " +
                "(espere /file/d/... o ?id=...).");
        }

        return GetFileContentAsync(fileId, cancellationToken);
    }

    public async Task<DriveFolderListing> ListFilesUnderFolderAsync(
        string folderId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId);

        var files = new List<DriveFile>();
        await foreach (var file in _listing
                           .ListFilesUnderFolderAsync(folderId, cancellationToken)
                           .ConfigureAwait(false))
        {
            files.Add(file);
        }

        return new DriveFolderListing(folderId, files);
    }

    private static string? TryExtractFileId(string driveUrl)
    {
        var m = FileDPath().Match(driveUrl);
        if (m.Success)
            return m.Groups[1].Value;

        m = IdQuery().Match(driveUrl);
        if (m.Success)
            return m.Groups[1].Value;

        return null;
    }

    [GeneratedRegex(@"/file/d/([a-zA-Z0-9_-]{10,})", RegexOptions.CultureInvariant)]
    private static partial Regex FileDPath();

    [GeneratedRegex(@"[?&]id=([a-zA-Z0-9_-]{10,})", RegexOptions.CultureInvariant)]
    private static partial Regex IdQuery();
}
