// ============================================================================
//  GoogleDriveClient ù Capa de I/O contra Google Drive (D3.1+)
// ----------------------------------------------------------------------------
//  Singleton que encapsula DriveService de Google.Apis.Drive.v3. Es el ùNICO
//  punto del backend que importa Google.Apis.*. El resto del agente accede a
//  Drive solo vùa el server MCP que envuelve a esta clase.
//
//  CONSTRUCCIùN PEREZOSA (idea rescatada del diff viejo):
//  El ctor NO carga las credenciales. Se cargan en el primer uso, dentro de
//  Lazy<DriveService>. Eso permite que la API levante aunque el JSON de
//  service account estù ausente o mal ù el error real (parseo, archivo no
//  existe, permisos) aparece reciùn cuando se invoca una tool MCP, con
//  mensaje claro. Si fallara en el ctor, serùa antes del DI y el error
//  quedarùa enmascarado.
// ============================================================================

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using DriveFileItem = Google.Apis.Drive.v3.Data.File;
using Google.Apis.Services;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Drive;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.API.Integrations.MCP.Drive;

public sealed class GoogleDriveClient : IDisposable
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";
    private const int MaxRecursionDepth = 10;

    /// <summary>Lockfiles temporales de Microsoft Office (~$documento.xlsx).</summary>
    private const string PrefijoLockfileOffice = "~$";

    /// <summary>
    /// Archivos basura de SO / sync que no deben aparecer en el listado.
    /// Comparaciùn por nombre exacto, case-insensitive.
    /// </summary>
    private static readonly HashSet<string> NombresArchivosBasuraExcluidos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "desktop.ini",
            "Thumbs.db",
            ".DS_Store"
        };

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
                "GoogleDrive:ServiceAccountKeyPath no estù configurada en appsettings.");
        }

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"No se encontrù el JSON de service account en '{path}'. " +
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
                "Verificar que el archivo sea un JSON vùlido de service account. " +
                $"Detalle: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Lista recursivamente todos los archivos bajo el folderId dado. Devuelve
    /// una lista plana con path relativo al folder raùz. Excluye carpetas y
    /// archivos en papelera.
    /// </summary>
    public async Task<IReadOnlyList<DriveFile>> ListFilesInFolderAsync(
        string folderId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId);

        var archivos = new List<DriveFile>();

        var (subcarpetasRecorridas, archivosFiltrados) = await RecorrerCarpetaAsync(
            folderId,
            pathPrefix: string.Empty,
            depth: 0,
            archivos,
            ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Listado recursivo de folder {FolderId}: {Archivos} archivos, " +
            "{Subcarpetas} subcarpetas recorridas, {Filtrados} archivos basura excluidos.",
            folderId, archivos.Count, subcarpetasRecorridas, archivosFiltrados);

        return archivos;
    }

    /// <returns>Cantidad de subcarpetas en las que se descendiù.</returns>
    private static bool EsArchivoBasuraSistema(string nombre) =>
        NombresArchivosBasuraExcluidos.Contains(nombre)
        || nombre.StartsWith(PrefijoLockfileOffice, StringComparison.Ordinal);

    private async Task<(int Subcarpetas, int Filtrados)> RecorrerCarpetaAsync(
        string folderId,
        string pathPrefix,
        int depth,
        List<DriveFile> archivos,
        CancellationToken ct)
    {
        IReadOnlyList<DriveFileItem> hijos;
        try
        {
            hijos = await ListarHijosDirectosAsync(folderId, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "No se pudo listar folderId={FolderId} (path relativo '{Path}'). " +
                "Se omite esta rama.",
                folderId, pathPrefix.TrimEnd('/'));
            return (0, 0);
        }

        var subcarpetasRecorridas = 0;
        var archivosFiltrados = 0;

        foreach (var hijo in hijos)
        {
            if (string.Equals(hijo.MimeType, FolderMimeType, StringComparison.Ordinal))
            {
                var carpetaPath = pathPrefix + hijo.Name;

                if (depth >= MaxRecursionDepth)
                {
                    _logger.LogWarning(
                        "Profundidad mùxima de recursiùn ({Max}) alcanzada en '{Path}'. " +
                        "No se desciende a subcarpetas.",
                        MaxRecursionDepth, carpetaPath);
                    continue;
                }

                subcarpetasRecorridas++;
                var (subEnRama, filtradosEnRama) = await RecorrerCarpetaAsync(
                    hijo.Id!,
                    pathPrefix: carpetaPath + "/",
                    depth: depth + 1,
                    archivos,
                    ct).ConfigureAwait(false);
                subcarpetasRecorridas += subEnRama;
                archivosFiltrados += filtradosEnRama;
                continue;
            }

            if (EsArchivoBasuraSistema(hijo.Name!))
            {
                archivosFiltrados++;
                continue;
            }

            var pathRelativo = pathPrefix + hijo.Name;
            archivos.Add(new DriveFile(
                Id: hijo.Id!,
                Name: hijo.Name!,
                MimeType: hijo.MimeType!,
                WebViewLink: hijo.WebViewLink,
                Size: hijo.Size,
                Path: pathRelativo));
        }

        return (subcarpetasRecorridas, archivosFiltrados);
    }

    /// <summary>
    /// Lista todos los hijos directos de un folder (archivos y carpetas), con
    /// paginaciùn completa. Excluye elementos en papelera.
    /// </summary>
    private async Task<IReadOnlyList<DriveFileItem>> ListarHijosDirectosAsync(
        string folderId, CancellationToken ct)
    {
        var resultado = new List<DriveFileItem>();
        string? pageToken = null;

        do
        {
            var request = _service.Value.Files.List();
            request.Q = $"'{folderId}' in parents and trashed = false";
            request.Fields = ListFields;
            request.PageSize = 100;
            request.PageToken = pageToken;
            request.SupportsAllDrives = true;
            request.IncludeItemsFromAllDrives = true;

            var response = await request.ExecuteAsync(ct).ConfigureAwait(false);

            if (response.Files is { Count: > 0 })
                resultado.AddRange(response.Files);

            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return resultado;
    }

    /// <summary>
    /// Descarga los bytes de un archivo + sus metadatos bùsicos.
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
