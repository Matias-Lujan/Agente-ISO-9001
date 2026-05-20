using System.Runtime.CompilerServices;
using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Mcp.Drive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ISOAuditAgent.DocumentAnalysis.Sources;

/// <summary>
/// Implementación de <see cref="IDocumentSource"/> que obtiene documentos
/// de Google Drive <b>únicamente vía cliente MCP</b> local (tools
/// <c>list_project_files</c> y <c>get_file_content</c>) — Fase E / RNF-03.
/// </summary>
public sealed class DriveDocumentSource : IDocumentSource
{
    private readonly IDriveMcpClient _mcp;
    private readonly ILogger<DriveDocumentSource> _logger;

    public DriveDocumentSource(
        IDriveMcpClient mcp,
        ILogger<DriveDocumentSource>? logger = null)
    {
        _mcp = mcp ?? throw new ArgumentNullException(nameof(mcp));
        _logger = logger ?? NullLogger<DriveDocumentSource>.Instance;
    }

    public FuenteDocumento Fuente => FuenteDocumento.Drive;

    public async IAsyncEnumerable<RawDocument> EnumerateAsync(
        int proyectoId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DriveDocumentSource: enumerando vía MCP para ProyectoId={ProyectoId}.",
            proyectoId);

        var emitidos = 0;
        var listing = await _mcp
            .ListProjectFilesAsync(proyectoId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var file in listing.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = await _mcp
                .GetFileContentAsync(file.Id, cancellationToken)
                .ConfigureAwait(false);

            var stream = new MemoryStream(content.Bytes, writable: false);

            var raw = new RawDocument(
                IdEnFuente: content.Id,
                NombreArchivo: content.Name,
                Fuente: Fuente,
                MimeType: content.MimeType,
                Tamano: content.Size ?? file.Size,
                FechaCreacion: file.CreatedTime,
                FechaModificacion: content.ModifiedTime ?? file.ModifiedTime,
                UrlReferencia: file.WebViewLink,
                Contenido: stream);

            emitidos++;
            yield return raw;
        }

        _logger.LogInformation(
            "DriveDocumentSource: emitidos {Cantidad} documentos (MCP) para ProyectoId={ProyectoId}.",
            emitidos, proyectoId);
    }
}
