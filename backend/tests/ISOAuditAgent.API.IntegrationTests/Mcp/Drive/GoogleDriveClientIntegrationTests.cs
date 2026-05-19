using ISOAuditAgent.API.Mcp.Drive;
using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.API.IntegrationTests.Mcp.Drive;

/// <summary>
/// Tests de integración contra una carpeta de prueba real en Google Drive.
/// Validan los criterios de aceptación de Fase 2: listado recursivo
/// respeta exclusiones y MIME, y descarga de un archivo devuelve bytes.
/// </summary>
/// <remarks>
/// Se omiten en CI por defecto. Para ejecutarlos en local:
/// <code>
/// $env:GOOGLE_DRIVE_TEST_CREDENTIALS_PATH = "C:\path\al\service-account.json"
/// $env:GOOGLE_DRIVE_TEST_FOLDER_ID = "1AbCdEfGhIjK..."
/// dotnet test backend/tests/ISOAuditAgent.API.IntegrationTests
/// </code>
/// La carpeta debe estar compartida con la service account configurada.
/// </remarks>
public class GoogleDriveClientIntegrationTests
{
    [GoogleDriveIntegrationFact]
    public async Task Lista_recursivamente_respeta_exclusiones_y_MIME()
    {
        var options = BuildOptionsFromEnvironment();
        var iOpts = Options.Create(options);

        using var client = new GoogleDriveClient(iOpts, NullLogger<GoogleDriveClient>.Instance);
        var resolver = new OptionsBackedProyectoDriveResolver(iOpts);
        var policy = new DriveExclusionPolicy(iOpts);
        var listing = new DriveListingService(resolver, client, policy, iOpts);

        var files = new List<DriveFile>();
        await foreach (var file in listing.ListFilesUnderProjectAsync(1))
        {
            files.Add(file);
        }

        Assert.All(files, f =>
            Assert.Contains(f.MimeType, options.Drive.MimeTypes));

        Assert.DoesNotContain(files, f =>
            options.Drive.Exclusiones.NombresExactos.Contains(f.Name));
    }

    [GoogleDriveIntegrationFact]
    public async Task Descarga_de_archivo_devuelve_bytes_coherentes()
    {
        var options = BuildOptionsFromEnvironment();
        var iOpts = Options.Create(options);

        using var client = new GoogleDriveClient(iOpts, NullLogger<GoogleDriveClient>.Instance);
        var resolver = new OptionsBackedProyectoDriveResolver(iOpts);
        var policy = new DriveExclusionPolicy(iOpts);
        var listing = new DriveListingService(resolver, client, policy, iOpts);

        DriveFile? candidate = null;
        await foreach (var file in listing.ListFilesUnderProjectAsync(1))
        {
            candidate = file;
            break;
        }

        Assert.NotNull(candidate);

        var content = await client.DownloadFileAsync(candidate!.Id);

        Assert.Equal(candidate.Id, content.Id);
        Assert.Equal(candidate.Name, content.Name);
        Assert.NotNull(content.Bytes);
        Assert.NotEmpty(content.Bytes);
        if (candidate.Size is long expected)
        {
            Assert.Equal(expected, content.Bytes.LongLength);
        }
    }

    private static DocumentAnalysisOptions BuildOptionsFromEnvironment()
    {
        var folderId = Environment.GetEnvironmentVariable(
            GoogleDriveIntegrationFactAttribute.FolderIdEnv)!;
        var credPath = Environment.GetEnvironmentVariable(
            GoogleDriveIntegrationFactAttribute.CredentialsPathEnv);
        var credJson = Environment.GetEnvironmentVariable(
            GoogleDriveIntegrationFactAttribute.CredentialsJsonEnv);

        return new DocumentAnalysisOptions
        {
            Drive = new DriveOptions
            {
                Mappings = new List<ProyectoFolderMapping>
                {
                    new() { ProyectoId = 1, FolderId = folderId }
                },
                MimeTypes = new List<string>
                {
                    "application/pdf",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                },
                Exclusiones = new DriveExclusionOptions
                {
                    NombresExactos = new List<string> { "Archivo", "Backup" },
                    PatronesRegex  = new List<string> { ".*archivo.*", ".*backup.*" }
                },
                Auth = new DriveAuthOptions
                {
                    ServiceAccountKeyPath = credPath,
                    ServiceAccountKeyJson = credJson,
                    ApplicationName = "ISOAuditAgent.IntegrationTests"
                }
            }
        };
    }
}
