using ISOAuditAgent.DocumentAnalysis.Drive;

namespace ISOAuditAgent.DocumentAnalysis.Mcp;

/// <summary>
/// Cliente MCP del servidor Drive local: expone las mismas capacidades que
/// <c>list_project_files</c> y <c>get_file_content</c> sin usar
/// <c>IDriveListingService</c> ni <c>IDriveClient</c> directamente (Fase E).
/// </summary>
public interface IDriveMcpClient
{
    /// <summary>
    /// Equivalente a la tool <c>list_project_files</c>.
    /// </summary>
    Task<DriveFileListing> ListProjectFilesAsync(
        int proyectoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Equivalente a la tool <c>get_file_content</c>.
    /// </summary>
    Task<DriveFileContent> GetFileContentAsync(
        string fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Equivalente a la tool <c>get_file_content_by_url</c> cuando la URL es de Drive.
    /// </summary>
    Task<DriveFileContent> GetFileContentByDriveUrlAsync(
        string driveUrl,
        CancellationToken cancellationToken = default);
}
