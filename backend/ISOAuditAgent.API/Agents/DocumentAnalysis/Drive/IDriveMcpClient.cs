// ============================================================================
//  IDriveMcpClient — Cliente MCP del server de Drive local (D3.2)
// ----------------------------------------------------------------------------
//  Abstracción que consume el agente DocumentAnalysis para hablar con Drive.
//  La implementación (DriveMcpSdkClient) habla por HTTP loopback con el
//  server MCP que vive en el mismo proceso (Integrations/MCP/Drive/).
//
//  ALCANCE D3.2: dos métodos, espejo 1:1 de las dos tools del server
//  expuestas en D3.1. Sin list_project_files ni get_file_content_by_url —
//  esos eran del diff viejo y mezclaban responsabilidades que el árbol
//  vigente resuelve antes (folderId viene del contrato 2, no del server MCP).
//
//  CONSUMIDORES FUTUROS:
//   - TailoringSource (D3.4): usa ambos métodos para localizar y descargar
//     el FR-29 desde el folder del proyecto.
//   - ArtefactoFisicoChecker (D3.5): usa ListFilesUnderFolderAsync para
//     búsqueda heurística + GetFileContentAsync para descarga.
// ============================================================================

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Drive;

public interface IDriveMcpClient
{
    /// <summary>
    /// Lista los archivos directos (sin recursión, sin subcarpetas) bajo el
    /// folderId indicado. Espejo de la tool MCP list_files_under_folder.
    /// </summary>
    Task<DriveFolderListing> ListFilesUnderFolderAsync(
        string folderId, CancellationToken ct = default);

    /// <summary>
    /// Descarga los bytes + metadatos de un archivo por su fileId. Espejo de
    /// la tool MCP get_file_content.
    /// </summary>
    Task<DriveFileContent> GetFileContentAsync(
        string fileId, CancellationToken ct = default);
}