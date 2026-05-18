using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ISOAuditAgent.API.Mcp.Drive;
using ISOAuditAgent.DocumentAnalysis.Drive;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;

namespace ISOAuditAgent.API.IntegrationTests.Mcp.Drive;

/// <summary>
/// Verifica que <see cref="DriveMcpTools"/> capture excepciones de la
/// cadena <c>resolver → listing → cliente Drive</c> y las relance como
/// <see cref="McpException"/> con un mensaje útil para el cliente MCP.
/// Son tests unitarios puros (no requieren credenciales).
/// </summary>
public sealed class DriveMcpToolsErrorVisibilityTests
{
    [Fact]
    public async Task ListProjectFiles_ProyectoSinMapeo_PropagaMcpExceptionConDetalle()
    {
        var tools = BuildTools(
            resolver: new ThrowingResolver(new ProyectoDriveMappingNotFoundException(42)));

        var ex = await Assert.ThrowsAsync<McpException>(
            () => tools.ListProjectFilesAsync(proyectoId: 42));

        Assert.Contains("list_project_files", ex.Message);
        Assert.Contains("ProyectoId=42", ex.Message);
        Assert.Contains("DocumentAnalysis:Drive:Mappings", ex.Message);
    }

    [Fact]
    public async Task ListProjectFiles_ListingFalla_PropagaMcpExceptionConTipoYMensaje()
    {
        var inner = new InvalidOperationException(
            "GoogleDriveClient no tiene credenciales configuradas.");

        var tools = BuildTools(
            resolver: new StubResolver("folder-XYZ"),
            listing: new ThrowingListingService(inner));

        var ex = await Assert.ThrowsAsync<McpException>(
            () => tools.ListProjectFilesAsync(proyectoId: 1));

        Assert.Contains("list_project_files", ex.Message);
        Assert.Contains("credenciales configuradas", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public async Task GetFileContent_ClienteFalla_PropagaMcpExceptionConCausa()
    {
        var inner = new HttpRequestException("403 Forbidden");
        var outer = new InvalidOperationException(
            "Error descargando archivo 'file-abc' desde Drive.", inner);

        var tools = BuildTools(client: new ThrowingDriveClient(outer));

        var ex = await Assert.ThrowsAsync<McpException>(
            () => tools.GetFileContentAsync(fileId: "file-abc"));

        Assert.Contains("get_file_content", ex.Message);
        Assert.Contains("Error descargando archivo", ex.Message);
        Assert.Contains("HttpRequestException", ex.Message);
        Assert.Contains("403 Forbidden", ex.Message);
    }

    [Fact]
    public async Task ListProjectFiles_OperacionCancelada_PropagaMcpExceptionLegible()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var tools = BuildTools(
            resolver: new StubResolver("folder-XYZ"),
            listing: new CancellingListingService());

        var ex = await Assert.ThrowsAsync<McpException>(
            () => tools.ListProjectFilesAsync(proyectoId: 1, cts.Token));

        Assert.Contains("list_project_files", ex.Message);
        Assert.Contains("cancelada", ex.Message);
    }

    private static DriveMcpTools BuildTools(
        IProyectoDriveResolver? resolver = null,
        IDriveListingService? listing = null,
        IDriveClient? client = null)
    {
        return new DriveMcpTools(
            listing ?? new EmptyListingService(),
            client ?? new EmptyDriveClient(),
            resolver ?? new StubResolver("folder-default"),
            NullLogger<DriveMcpTools>.Instance);
    }

    private sealed class StubResolver : IProyectoDriveResolver
    {
        private readonly string _folderId;

        public StubResolver(string folderId) => _folderId = folderId;

        public string ResolveFolderId(int proyectoId) => _folderId;

        public bool TryResolveFolderId(int proyectoId, [NotNullWhen(true)] out string? folderId)
        {
            folderId = _folderId;
            return true;
        }
    }

    private sealed class ThrowingResolver : IProyectoDriveResolver
    {
        private readonly Exception _ex;

        public ThrowingResolver(Exception ex) => _ex = ex;

        public string ResolveFolderId(int proyectoId) => throw _ex;

        public bool TryResolveFolderId(int proyectoId, [NotNullWhen(true)] out string? folderId)
        {
            folderId = null;
            return false;
        }
    }

    private sealed class EmptyListingService : IDriveListingService
    {
        public async IAsyncEnumerable<DriveFile> ListFilesUnderProjectAsync(
            int proyectoId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public async IAsyncEnumerable<DriveFile> ListFilesUnderFolderAsync(
            string folderId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class ThrowingListingService : IDriveListingService
    {
        private readonly Exception _ex;

        public ThrowingListingService(Exception ex) => _ex = ex;

        public IAsyncEnumerable<DriveFile> ListFilesUnderProjectAsync(
            int proyectoId,
            CancellationToken cancellationToken = default)
            => throw _ex;

        public IAsyncEnumerable<DriveFile> ListFilesUnderFolderAsync(
            string folderId,
            CancellationToken cancellationToken = default)
            => throw _ex;
    }

    private sealed class CancellingListingService : IDriveListingService
    {
        public async IAsyncEnumerable<DriveFile> ListFilesUnderProjectAsync(
            int proyectoId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield break;
        }

        public async IAsyncEnumerable<DriveFile> ListFilesUnderFolderAsync(
            string folderId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield break;
        }
    }

    private sealed class EmptyDriveClient : IDriveClient
    {
        public async IAsyncEnumerable<DriveItem> ListChildrenAsync(
            string folderId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task<DriveFileContent> DownloadFileAsync(
            string fileId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                new DriveFileContent(
                    Id: fileId,
                    Name: string.Empty,
                    MimeType: string.Empty,
                    Bytes: Array.Empty<byte>(),
                    Size: null,
                    ModifiedTime: null));
    }

    private sealed class ThrowingDriveClient : IDriveClient
    {
        private readonly Exception _ex;

        public ThrowingDriveClient(Exception ex) => _ex = ex;

        public IAsyncEnumerable<DriveItem> ListChildrenAsync(
            string folderId,
            CancellationToken cancellationToken = default)
            => throw _ex;

        public Task<DriveFileContent> DownloadFileAsync(
            string fileId,
            CancellationToken cancellationToken = default)
            => Task.FromException<DriveFileContent>(_ex);
    }
}
