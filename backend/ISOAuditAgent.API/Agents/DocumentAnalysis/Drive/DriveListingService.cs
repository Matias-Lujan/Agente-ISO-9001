using System.Runtime.CompilerServices;
using ISOAuditAgent.DocumentAnalysis.Configuration;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Drive;

/// <summary>
/// Implementación de <see cref="IDriveListingService"/> que delega a un
/// <see cref="IDriveClient"/> y aplica
/// <see cref="DriveExclusionPolicy"/> + filtro MIME en memoria.
/// </summary>
/// <remarks>
/// <para>
/// Recorre el subárbol con BFS para que el primer archivo emitido tenga
/// la latencia más baja posible (los archivos en la raíz aparecen antes
/// que los de subcarpetas profundas).
/// </para>
/// <para>
/// La traversal poda subárboles enteros cuando una carpeta es excluida
/// (no descarga sus hijos). El filtro MIME es <c>OrdinalIgnoreCase</c>.
/// </para>
/// </remarks>
public sealed class DriveListingService : IDriveListingService
{
    private readonly IProyectoDriveResolver _resolver;
    private readonly IDriveClient _client;
    private readonly DriveExclusionPolicy _exclusionPolicy;
    private readonly HashSet<string> _allowedMimeTypes;

    public DriveListingService(
        IProyectoDriveResolver resolver,
        IDriveClient client,
        DriveExclusionPolicy exclusionPolicy,
        IOptions<DocumentAnalysisOptions> options)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _exclusionPolicy = exclusionPolicy ?? throw new ArgumentNullException(nameof(exclusionPolicy));

        ArgumentNullException.ThrowIfNull(options);

        _allowedMimeTypes = new HashSet<string>(
            options.Value.Drive.MimeTypes,
            StringComparer.OrdinalIgnoreCase);
    }

    public IAsyncEnumerable<DriveFile> ListFilesUnderProjectAsync(
        int proyectoId,
        CancellationToken cancellationToken = default)
    {
        var rootFolderId = _resolver.ResolveFolderId(proyectoId);
        return ListFilesUnderFolderAsync(rootFolderId, cancellationToken);
    }

    public async IAsyncEnumerable<DriveFile> ListFilesUnderFolderAsync(
        string folderId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId);

        var queue = new Queue<string>();
        queue.Enqueue(folderId);

        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = queue.Dequeue();

            await foreach (var item in _client.ListChildrenAsync(current, cancellationToken)
                .ConfigureAwait(false))
            {
                switch (item)
                {
                    case DriveFolder folder:
                        if (!_exclusionPolicy.IsFolderExcluded(folder.Name))
                        {
                            queue.Enqueue(folder.Id);
                        }
                        break;

                    case DriveFile file when MimeTypeAllowed(file.MimeType):
                        yield return file;
                        break;
                }
            }
        }
    }

    private bool MimeTypeAllowed(string mimeType)
    {
        // Si la lista está vacía, ningún MIME pasa: la configuración debe
        // declarar explícitamente los formatos aceptados (PDF/DOCX/XLSX
        // por defecto en appsettings.json).
        return _allowedMimeTypes.Count > 0 && _allowedMimeTypes.Contains(mimeType);
    }
}
