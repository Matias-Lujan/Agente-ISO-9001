namespace ISOAuditAgent.DocumentAnalysis.Drive;

/// <summary>
/// Servicio de alto nivel que dado un <c>ProyectoId</c> resuelve la
/// carpeta raíz en Drive (vía <see cref="IProyectoDriveResolver"/>) y
/// enumera todos los archivos candidatos del subárbol, aplicando
/// filtros de MIME y exclusiones de carpetas.
/// </summary>
/// <remarks>
/// Independiente del transporte (MCP / cliente directo). Lo consume el
/// servidor MCP (Fase 2) y, eventualmente, podría consumirlo directamente
/// el runner si en el futuro se decidiera saltar el MCP.
/// </remarks>
public interface IDriveListingService
{
    /// <summary>
    /// Enumera, en orden de descubrimiento, los archivos del proyecto que
    /// satisfacen el filtro de MIME types y no están bajo carpetas excluidas.
    /// </summary>
    /// <exception cref="ProyectoDriveMappingNotFoundException">
    /// No existe un mapeo de carpeta para <paramref name="proyectoId"/>.
    /// </exception>
    IAsyncEnumerable<DriveFile> ListFilesUnderProjectAsync(
        int proyectoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enumera, en orden de descubrimiento, los archivos bajo
    /// <paramref name="folderId"/> que satisfacen el filtro de MIME types
    /// y no están bajo carpetas excluidas. No requiere mapeo de proyecto.
    /// </summary>
    /// <remarks>
    /// Útil para la carpeta de templates del sistema
    /// (<c>configuracion_sistema.drive_carpeta_templates_id</c>) y otras
    /// carpetas resueltas por código (no por <c>ProyectoId</c>).
    /// </remarks>
    IAsyncEnumerable<DriveFile> ListFilesUnderFolderAsync(
        string folderId,
        CancellationToken cancellationToken = default);
}
