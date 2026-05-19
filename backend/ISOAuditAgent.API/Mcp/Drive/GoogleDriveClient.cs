using System.Runtime.CompilerServices;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DriveItem = ISOAuditAgent.DocumentAnalysis.Drive.DriveItem;
using DriveFile = ISOAuditAgent.DocumentAnalysis.Drive.DriveFile;
using DriveFolder = ISOAuditAgent.DocumentAnalysis.Drive.DriveFolder;
using GoogleFile = Google.Apis.Drive.v3.Data.File;
using GoogleFileList = Google.Apis.Drive.v3.Data.FileList;

namespace ISOAuditAgent.API.Mcp.Drive;

/// <summary>
/// Implementación de <see cref="IDriveClient"/> basada en
/// <c>Google.Apis.Drive.v3</c> con autenticación de service account
/// (§7.1 de la especificación).
/// </summary>
/// <remarks>
/// <para>
/// Vive en <c>ISOAuditAgent.API</c> a propósito: la abstracción
/// <see cref="IDriveClient"/> está en la librería
/// <c>ISOAuditAgent.DocumentAnalysis</c> y permanece libre de
/// dependencias de Google.
/// </para>
/// <para>
/// La construcción del <see cref="DriveService"/> es <b>perezosa</b>:
/// el ctor no toca las credenciales. Esto es deliberado para que:
/// (a) la API pueda arrancar aún si el JSON de service account es inválido
/// o falta; (b) el error real (parseo de JSON, archivo inexistente,
/// permisos, etc.) ocurra dentro de la invocación de la herramienta MCP
/// donde <c>DriveMcpTools</c> tiene un <c>try/catch</c> que lo expone al
/// cliente vía <c>McpException</c>. Si la inicialización tirara en el ctor,
/// la excepción ocurriría durante el resolve de DI, antes del catch de la
/// tool, y MCP enmascararía el mensaje al cliente.
/// </para>
/// </remarks>
public sealed class GoogleDriveClient : IDriveClient, IDisposable
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    private static readonly string FileFields =
        "id, name, mimeType, webViewLink, size, createdTime, modifiedTime";

    private static readonly string ListFields =
        $"nextPageToken, files({FileFields})";

    private readonly DriveAuthOptions _auth;
    private readonly Lazy<DriveService> _service;
    private readonly ILogger<GoogleDriveClient> _logger;

    public GoogleDriveClient(
        IOptions<DocumentAnalysisOptions> options,
        ILogger<GoogleDriveClient> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _auth = options.Value.Drive.Auth;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _service = new Lazy<DriveService>(
            BuildService,
            LazyThreadSafetyMode.ExecutionAndPublication);

        if (!_auth.HasCredentials)
        {
            _logger.LogWarning(
                "GoogleDriveClient inicializado SIN credenciales " +
                "(DocumentAnalysis:Drive:Auth). Las llamadas reales fallarán " +
                "hasta que se configure ServiceAccountKeyPath o ServiceAccountKeyJson.");
        }
    }

    private DriveService BuildService()
    {
        if (!_auth.HasCredentials)
        {
            throw new InvalidOperationException(
                "GoogleDriveClient no tiene credenciales configuradas. " +
                "Defina DocumentAnalysis:Drive:Auth:ServiceAccountKeyPath o " +
                "DocumentAnalysis:Drive:Auth:ServiceAccountKeyJson en appsettings.");
        }

        try
        {
            var credential = LoadCredential(_auth)
                .CreateScoped(DriveService.ScopeConstants.DriveReadonly);

            return new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _auth.ApplicationName
            });
        }
        catch (Exception ex)
        {
            var source = !string.IsNullOrWhiteSpace(_auth.ServiceAccountKeyPath)
                ? $"archivo '{_auth.ServiceAccountKeyPath}'"
                : "JSON inline en DocumentAnalysis:Drive:Auth:ServiceAccountKeyJson";

            _logger.LogError(ex,
                "No se pudo cargar la credencial de service account desde {Source}.",
                source);

            throw new InvalidOperationException(
                $"No se pudo cargar la credencial de service account de Google Drive desde {source}. " +
                "Verifique que sea un JSON de service account válido (no apunte a appsettings.json " +
                "ni a un archivo con BOM/comentarios) y que la ruta exista. " +
                $"Detalle: {ex.Message}",
                ex);
        }
    }

    public async IAsyncEnumerable<DriveItem> ListChildrenAsync(
        string folderId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderId);
        var service = EnsureService();

        string? pageToken = null;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = service.Files.List();
            request.Q = $"'{folderId}' in parents and trashed = false";
            request.Fields = ListFields;
            request.PageToken = pageToken;
            request.PageSize = 100;
            request.SupportsAllDrives = true;
            request.IncludeItemsFromAllDrives = true;

            GoogleFileList response;
            try
            {
                response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error listando hijos de la carpeta {FolderId} en Google Drive.",
                    folderId);
                throw;
            }

            if (response.Files is not null)
            {
                foreach (var file in response.Files)
                {
                    yield return Map(file);
                }
            }

            pageToken = response.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));
    }

    public async Task<DriveFileContent> DownloadFileAsync(
        string fileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileId);
        var service = EnsureService();

        var metadataRequest = service.Files.Get(fileId);
        metadataRequest.Fields = FileFields;
        metadataRequest.SupportsAllDrives = true;

        var metadata = await metadataRequest
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        var contentRequest = service.Files.Get(fileId);
        contentRequest.SupportsAllDrives = true;

        using var stream = new MemoryStream();
        await contentRequest
            .DownloadAsync(stream, cancellationToken)
            .ConfigureAwait(false);

        return new DriveFileContent(
            Id: metadata.Id,
            Name: metadata.Name,
            MimeType: metadata.MimeType,
            Bytes: stream.ToArray(),
            Size: metadata.Size,
            ModifiedTime: ToDateTimeOffset(metadata.ModifiedTimeRaw)
        );
    }

    public void Dispose()
    {
        if (_service.IsValueCreated)
        {
            _service.Value.Dispose();
        }
    }

    private DriveService EnsureService() => _service.Value;

    private static GoogleCredential LoadCredential(DriveAuthOptions auth)
    {
        // CredentialFactory tipado: mitigación "Windy Eagle" — exige declarar el
        // tipo esperado para evitar credenciales inesperadas en el JSON.
        ServiceAccountCredential serviceAccount =
            !string.IsNullOrWhiteSpace(auth.ServiceAccountKeyJson)
                ? CredentialFactory.FromJson<ServiceAccountCredential>(auth.ServiceAccountKeyJson)
                : CredentialFactory.FromFile<ServiceAccountCredential>(auth.ServiceAccountKeyPath!);

        return serviceAccount.ToGoogleCredential();
    }

    private static DriveItem Map(GoogleFile file)
    {
        if (string.Equals(file.MimeType, FolderMimeType, StringComparison.Ordinal))
        {
            return new DriveFolder(file.Id, file.Name);
        }

        return new DriveFile(
            Id: file.Id,
            Name: file.Name,
            MimeType: file.MimeType,
            WebViewLink: file.WebViewLink,
            Size: file.Size,
            CreatedTime: ToDateTimeOffset(file.CreatedTimeRaw),
            ModifiedTime: ToDateTimeOffset(file.ModifiedTimeRaw)
        );
    }

    private static DateTimeOffset? ToDateTimeOffset(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        return DateTimeOffset.TryParse(raw, out var parsed) ? parsed : null;
    }
}
