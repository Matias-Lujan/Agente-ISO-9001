using System.Runtime.CompilerServices;
using ISOAuditAgent.DocumentAnalysis.Drive;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Drive;

/// <summary>
/// Implementación en memoria de <see cref="IDriveClient"/> para tests
/// del <c>DriveListingService</c> y de las herramientas MCP.
/// El árbol se modela con un <see cref="Dictionary{TKey,TValue}"/>
/// <c>folderId → hijos directos</c>.
/// </summary>
internal sealed class FakeDriveClient : IDriveClient
{
    private readonly Dictionary<string, IReadOnlyList<DriveItem>> _tree;
    private readonly Dictionary<string, DriveFileContent> _contents;

    public FakeDriveClient(
        Dictionary<string, IReadOnlyList<DriveItem>> tree,
        Dictionary<string, DriveFileContent>? contents = null)
    {
        _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        _contents = contents ?? new Dictionary<string, DriveFileContent>();
    }

    public List<string> VisitedFolders { get; } = new();

    public async IAsyncEnumerable<DriveItem> ListChildrenAsync(
        string folderId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        VisitedFolders.Add(folderId);

        if (!_tree.TryGetValue(folderId, out var children))
        {
            yield break;
        }

        foreach (var child in children)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return child;
        }
    }

    public Task<DriveFileContent> DownloadFileAsync(
        string fileId,
        CancellationToken cancellationToken = default)
    {
        if (_contents.TryGetValue(fileId, out var content))
        {
            return Task.FromResult(content);
        }

        throw new InvalidOperationException(
            $"FakeDriveClient: no se configuró contenido para fileId='{fileId}'.");
    }
}
