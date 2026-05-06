using System.Runtime.CompilerServices;
using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Drive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ISOAuditAgent.DocumentAnalysis.Sources;

/// <summary>
/// Implementación de <see cref="IDocumentSource"/> que toma como fuente
/// Google Drive a través de las abstracciones MCP del propio módulo
/// (<see cref="IDriveListingService"/> + <see cref="IDriveClient"/>).
/// </summary>
/// <remarks>
/// <para>
/// Para cada archivo descubierto por el listing recursivo descarga los
/// bytes y los entrega como <see cref="RawDocument"/>. La política de
/// "saltar archivo si falla" pertenece a la Fase 5 (runner): aquí los
/// errores se propagan al consumidor para que decida.
/// </para>
/// <para>
/// El <c>FolderId</c> raíz lo resuelve <see cref="IDriveListingService"/>
/// llamando a <see cref="IProyectoDriveResolver"/>; este source no lo
/// resuelve directamente para evitar duplicar la lógica.
/// </para>
/// </remarks>
public sealed class DriveDocumentSource : IDocumentSource
{
    private readonly IDriveListingService _listing;
    private readonly IDriveClient _client;
    private readonly ILogger<DriveDocumentSource> _logger;

    public DriveDocumentSource(
        IDriveListingService listing,
        IDriveClient client,
        ILogger<DriveDocumentSource>? logger = null)
    {
        _listing = listing ?? throw new ArgumentNullException(nameof(listing));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? NullLogger<DriveDocumentSource>.Instance;
    }

    public FuenteDocumento Fuente => FuenteDocumento.GoogleDrive;

    public async IAsyncEnumerable<RawDocument> EnumerateAsync(
        int proyectoId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DriveDocumentSource: enumerando documentos para ProyectoId={ProyectoId}.",
            proyectoId);

        var emitidos = 0;

        await foreach (var file in _listing
            .ListFilesUnderProjectAsync(proyectoId, cancellationToken)
            .ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = await _client
                .DownloadFileAsync(file.Id, cancellationToken)
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
            "DriveDocumentSource: emitidos {Cantidad} documentos para ProyectoId={ProyectoId}.",
            emitidos, proyectoId);
    }
}
