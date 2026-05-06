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
}
