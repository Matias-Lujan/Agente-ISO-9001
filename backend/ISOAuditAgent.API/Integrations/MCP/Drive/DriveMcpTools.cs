// ============================================================================
//  DriveMcpTools — Tools MCP expuestas por el server in-process (D3.1)
// ----------------------------------------------------------------------------
//  Dos tools mínimas:
//    list_files_under_folder(folderId)  → DriveFolderListing
//    get_file_content(fileId)           → DriveFileContent (bytes en base64)
//
//  Se montan en /mcp/drive vía DriveMcpExtensions + app.MapMcp.
//
//  POR QUÉ ESTAS DOS Y NO MÁS:
//  El diff viejo exponía 4 tools, pero dos de ellas (list_project_files,
//  get_file_content_by_url) dependían de un resolver proyectoId→folderId y
//  un parser de URLs de Drive que mezclan responsabilidades. En el árbol
//  vigente el folderId ya viene del contrato 2 (Integraciones.DriveFolderId)
//  resuelto por ResolutorContextoService. El server MCP se queda con la
//  responsabilidad mínima: hablar con Drive.
// ============================================================================

using System.ComponentModel;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Drive;
using ModelContextProtocol.Server;

namespace ISOAuditAgent.API.Integrations.MCP.Drive;

[McpServerToolType]
public sealed class DriveMcpTools
{
    private readonly GoogleDriveClient _client;
    private readonly ILogger<DriveMcpTools> _logger;

    public DriveMcpTools(GoogleDriveClient client, ILogger<DriveMcpTools> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [McpServerTool(Name = "list_files_under_folder")]
    [Description(
        "Lista recursivamente todos los archivos bajo el folderId de Google " +
        "Drive indicado (hasta 10 niveles de subcarpetas). Devuelve una lista " +
        "plana solo de archivos (sin carpetas), cada uno con id, name, " +
        "mimeType, webViewLink, size y path relativo al folder raíz.")]
    public async Task<DriveFolderListing> ListFilesUnderFolderAsync(
        [Description("FolderId de Google Drive a listar.")]
        string folderId,
        CancellationToken ct = default)
    {
        try
        {
            var files = await _client.ListFilesInFolderAsync(folderId, ct);
            _logger.LogInformation(
                "MCP list_files_under_folder: folderId={FolderId}, {Count} archivos.",
                folderId, files.Count);
            return new DriveFolderListing(folderId, files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "MCP tool list_files_under_folder falló con folderId={FolderId}.",
                folderId);
            throw;
        }
    }

    [McpServerTool(Name = "get_file_content")]
    [Description(
        "Descarga los bytes y metadatos de un archivo de Drive identificado " +
        "por su fileId. Los bytes se devuelven codificados en base64 (auto " +
        "por la serialización JSON del SDK MCP).")]
    public async Task<DriveFileContent> GetFileContentAsync(
        [Description("FileId de Google Drive.")]
        string fileId,
        CancellationToken ct = default)
    {
        try
        {
            return await _client.DownloadFileAsync(fileId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "MCP tool get_file_content falló con fileId={FileId}.",
                fileId);
            throw;
        }
    }
}