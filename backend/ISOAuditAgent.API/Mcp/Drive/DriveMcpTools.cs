using System.ComponentModel;
using ISOAuditAgent.DocumentAnalysis.Drive;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ISOAuditAgent.API.Mcp.Drive;

/// <summary>
/// Herramientas MCP expuestas bajo <c>/mcp/drive</c> (§7.5-7.6 de la
/// especificación). Mínimo requerido por la Fase 2:
/// listar archivos candidatos del proyecto y obtener el contenido binario
/// de un archivo.
/// </summary>
/// <remarks>
/// Cada tool atrapa cualquier excepción de la cadena
/// <c>resolver → listing → cliente Drive</c> y la relanza como
/// <see cref="McpException"/> para que el cliente MCP reciba un mensaje
/// útil (en vez del genérico "An error occurred invoking ..."). El stack
/// trace completo queda en los logs del host.
/// </remarks>
[McpServerToolType]
public sealed class DriveMcpTools
{
    private readonly IDriveListingService _listing;
    private readonly IDriveClient _client;
    private readonly IProyectoDriveResolver _resolver;
    private readonly ILogger<DriveMcpTools> _logger;

    public DriveMcpTools(
        IDriveListingService listing,
        IDriveClient client,
        IProyectoDriveResolver resolver,
        ILogger<DriveMcpTools> logger)
    {
        _listing = listing ?? throw new ArgumentNullException(nameof(listing));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [McpServerTool(Name = "list_project_files")]
    [Description(
        "Lista recursivamente los archivos candidatos (PDF/DOCX/XLSX por defecto) " +
        "asociados al ProyectoId indicado. Aplica filtro de MIME types y poda " +
        "las carpetas excluidas configuradas en appsettings.")]
    public async Task<DriveFileListing> ListProjectFilesAsync(
        [Description("Identificador del proyecto en la base de datos del sistema.")]
        int proyectoId,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "list_project_files";

        try
        {
            var folderId = _resolver.ResolveFolderId(proyectoId);

            var files = new List<DriveFile>();
            await foreach (var file in _listing
                .ListFilesUnderProjectAsync(proyectoId, cancellationToken)
                .ConfigureAwait(false))
            {
                files.Add(file);
            }

            return new DriveFileListing(proyectoId, folderId, files);
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invocando MCP tool '{ToolName}' con ProyectoId={ProyectoId}.",
                toolName, proyectoId);
            throw ex.AsToolError(toolName);
        }
    }

    [McpServerTool(Name = "get_file_content")]
    [Description(
        "Descarga los bytes y los metadatos de un archivo de Google Drive " +
        "identificado por su fileId. Los bytes se devuelven codificados en base64.")]
    public async Task<DriveFileContent> GetFileContentAsync(
        [Description("Identificador del archivo en Google Drive (file id).")]
        string fileId,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "get_file_content";

        try
        {
            return await _client
                .DownloadFileAsync(fileId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invocando MCP tool '{ToolName}' con fileId={FileId}.",
                toolName, fileId);
            throw ex.AsToolError(toolName);
        }
    }

    [McpServerTool(Name = "list_files_under_folder")]
    [Description(
        "Lista recursivamente los archivos candidatos (PDF/DOCX/XLSX por defecto) " +
        "bajo el folderId indicado de Google Drive. No requiere ProyectoId. " +
        "Útil para resolver la carpeta de templates del sistema.")]
    public async Task<DriveFolderListing> ListFilesUnderFolderAsync(
        [Description("FolderId de Google Drive a recorrer recursivamente.")]
        string folderId,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "list_files_under_folder";

        try
        {
            var files = new List<DriveFile>();
            await foreach (var file in _listing
                .ListFilesUnderFolderAsync(folderId, cancellationToken)
                .ConfigureAwait(false))
            {
                files.Add(file);
            }

            return new DriveFolderListing(folderId, files);
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invocando MCP tool '{ToolName}' con folderId={FolderId}.",
                toolName, folderId);
            throw ex.AsToolError(toolName);
        }
    }

    [McpServerTool(Name = "get_file_content_by_url")]
    [Description(
        "Descarga un archivo de Google Drive a partir de su URL de " +
        "visualización o enlace compartido (extrae el fileId automáticamente).")]
    public async Task<DriveFileContent> GetFileContentByDriveUrlAsync(
        [Description("URL https://drive.google.com/... del archivo.")]
        string driveUrl,
        CancellationToken cancellationToken = default)
    {
        const string toolName = "get_file_content_by_url";

        try
        {
            var fileId = GoogleDriveShareUrl.TryExtractFileId(driveUrl);
            if (string.IsNullOrEmpty(fileId))
            {
                throw new InvalidOperationException(
                    "La URL no contiene un identificador de archivo de Drive reconocible " +
                    "(espere /file/d/... o ?id=...).");
            }

            return await _client
                .DownloadFileAsync(fileId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invocando MCP tool '{ToolName}' con driveUrl.",
                toolName);
            throw ex.AsToolError(toolName);
        }
    }
}
