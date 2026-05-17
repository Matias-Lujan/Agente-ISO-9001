namespace ISOAuditAgent.DocumentAnalysis.Drive;

/// <summary>
/// Abstracción de bajo nivel sobre Google Drive: enumera hijos directos
/// de una carpeta y descarga bytes por <c>fileId</c>.
/// </summary>
/// <remarks>
/// <para>
/// La recursión, filtrado por MIME y aplicación de exclusiones de
/// carpetas son responsabilidad de capas superiores
/// (<see cref="DriveExclusionPolicy"/>, <see cref="IDriveListingService"/>).
/// </para>
/// <para>
/// Implementaciones disponibles:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>GoogleDriveClient</c> (en <c>ISOAuditAgent.API</c>): cliente real
///     basado en <c>Google.Apis.Drive.v3</c> con autenticación de
///     service account.
///   </description></item>
///   <item><description>
///     <c>InMemoryDriveClient</c> (en tests): árbol estático para pruebas
///     unitarias sin red.
///   </description></item>
/// </list>
/// </remarks>
public interface IDriveClient
{
    /// <summary>
    /// Devuelve los hijos directos (archivos y subcarpetas) de
    /// <paramref name="folderId"/>.
    /// </summary>
    IAsyncEnumerable<DriveItem> ListChildrenAsync(
        string folderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Descarga el contenido binario y los metadatos del archivo
    /// identificado por <paramref name="fileId"/>.
    /// </summary>
    Task<DriveFileContent> DownloadFileAsync(
        string fileId,
        CancellationToken cancellationToken = default);
}
