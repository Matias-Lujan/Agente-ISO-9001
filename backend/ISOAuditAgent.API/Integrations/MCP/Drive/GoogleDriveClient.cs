// ============================================================================
//  GoogleDriveClient — Capa de I/O contra Google Drive (D3.1)
// ----------------------------------------------------------------------------
//  Singleton que encapsula DriveService de Google.Apis.Drive.v3. Es el ÚNICO
//  punto del backend que importa Google.Apis.*. El resto del agente accede a
//  Drive solo vía el server MCP que envuelve a esta clase.
//
//  CONSTRUCCIÓN PEREZOSA (idea rescatada del diff viejo):
//  El ctor NO carga las credenciales. Se cargan en el primer uso, dentro de
//  Lazy<DriveService>. Eso permite que la API levante aunque el JSON de
//  service account esté ausente o mal — el error real (parseo, archivo no
//  existe, permisos) aparece recién cuando se invoca una tool MCP, con
//  mensaje claro. Si fallara en el ctor, sería antes del DI y el error
//  quedaría enmascarado.
//
//  ALCANCE D3.1: solo dos operaciones — listar hijos directos de un folder
//  (sin recursión, una página de hasta 100 archivos; suficiente para el smoke)
//  y descargar bytes. El listado recursivo y la paginación completa son D3.2+.
// ============================================================================

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Drive;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.API.Integrations.MCP.Drive;

public sealed class GoogleDriveClient : IDisposable
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    private static readonly string FileFields =
        "id, name, mimeType, webViewLink, size";

    private static readonly string ListFields =
        $"nextPageToken, files({FileFields})";

    private readonly GoogleDriveOptions _options;
    private readonly ILogger<GoogleDriveClient> _logger;
    private readonly Lazy<DriveService> _service;

    public GoogleDriveClient(
        IOptions<GoogleDriveOptions> options,
        ILogger<GoogleDriveClient> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _service = new Lazy<DriveService>(
            BuildService,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private DriveService BuildService()
    {
        var path = _options.ServiceAccountKeyPath;

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                "GoogleDrive:ServiceAccountKeyPath no está configurada en appsettings.");
        }

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"No se encontró el JSON de service account en '{path}'. " +
                "Verificar GoogleDrive:ServiceAccountKeyPath y que el archivo exista " +
                "relativo al working directory de la API.");
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var credential = GoogleCredential
                .FromStream(stream)
                .CreateScoped(DriveService.ScopeConstants.DriveReadonly);

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _options.ApplicationName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "No se pudo cargar la credencial de service account desde '{Path}'.",
                path);

            throw new InvalidOperationException(
                $"No se pudo cargar la credencial de service account desde '{path}'. " +
                "Verificar que el archivo sea un JSON válido de service account. " +
                $"Detalle: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Lista los archivos directos (sin recursión) bajo el folderId dado.
    /// En D3.1 solo se necesita una página — paginación completa va en D3.2.
    /// </summary>
    public async Task<IReadOnlyList<DriveFile>> ListFilesInFolderAsync(
        string folderId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId);

        var request = _service.Value.Files.List();
        request.Q = $"'{folderId}' in parents and trashed = false";
        request.Fields = ListFields;
        request.PageSize = 100;
        request.SupportsAllDrives = true;
        request.IncludeItemsFromAllDrives = true;

        var response = await request.ExecuteAsync(ct).ConfigureAwait(false);

        if (response.Files is null || response.Files.Count == 0)
        {
            return Array.Empty<DriveFile>();
        }

        var resultado = new List<DriveFile>(response.Files.Count);
        foreach (var f in response.Files)
        {
            // Filtrar carpetas — D3.1 lista solo archivos. Subcarpetas se
            // recorren en D3.2 con listado recursivo.
            if (string.Equals(f.MimeType, FolderMimeType, StringComparison.Ordinal))
                continue;

            resultado.Add(new DriveFile(
                Id: f.Id,
                Name: f.Name,
                MimeType: f.MimeType,
                WebViewLink: f.WebViewLink,
                Size: f.Size));
        }

        return resultado;
    }

    /// <summary>
    /// Descarga los bytes de un archivo + sus metadatos básicos.
    /// </summary>
    public async Task<DriveFileContent> DownloadFileAsync(
        string fileId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);

        var metadataRequest = _service.Value.Files.Get(fileId);
        metadataRequest.Fields = FileFields;
        metadataRequest.SupportsAllDrives = true;
        var metadata = await metadataRequest.ExecuteAsync(ct).ConfigureAwait(false);

        var contentRequest = _service.Value.Files.Get(fileId);
        contentRequest.SupportsAllDrives = true;

        using var stream = new MemoryStream();
        await contentRequest.DownloadAsync(stream, ct).ConfigureAwait(false);

        return new DriveFileContent(
            Id: metadata.Id,
            Name: metadata.Name,
            MimeType: metadata.MimeType,
            Bytes: stream.ToArray(),
            Size: metadata.Size);
    }

    public void Dispose()
    {
        if (_service.IsValueCreated)
        {
            _service.Value.Dispose();
        }
    }
}