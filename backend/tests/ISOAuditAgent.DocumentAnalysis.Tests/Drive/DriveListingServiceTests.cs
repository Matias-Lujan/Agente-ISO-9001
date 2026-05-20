using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Drive;

/// <summary>
/// Tests de <see cref="DriveListingService"/>: cubren el criterio de
/// aceptación de Fase 2 ("listado recursivo respeta exclusiones y MIME")
/// usando un <see cref="FakeDriveClient"/> en memoria.
/// </summary>
public class DriveListingServiceTests
{
    private const string Pdf  = "application/pdf";
    private const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string Png  = "image/png";

    [Fact]
    public async Task Lista_recursivamente_archivos_validos_de_subarbol()
    {
        var fake = new FakeDriveClient(new Dictionary<string, IReadOnlyList<DriveItem>>
        {
            ["root"] = new DriveItem[]
            {
                File("f1", "manual.pdf", Pdf),
                new DriveFolder("sub1", "Procesos")
            },
            ["sub1"] = new DriveItem[]
            {
                File("f2", "registro.docx", Docx),
                new DriveFolder("sub2", "Indicadores")
            },
            ["sub2"] = new DriveItem[]
            {
                File("f3", "kpi.xlsx", Xlsx)
            }
        });

        var service = BuildService(fake, mimeTypes: new[] { Pdf, Docx, Xlsx });

        var files = await CollectAsync(service.ListFilesUnderProjectAsync(1));

        Assert.Equal(new[] { "f1", "f2", "f3" }, files.Select(f => f.Id).ToArray());
        Assert.Contains("root", fake.VisitedFolders);
        Assert.Contains("sub1", fake.VisitedFolders);
        Assert.Contains("sub2", fake.VisitedFolders);
    }

    [Fact]
    public async Task Filtra_archivos_con_MimeType_no_permitido()
    {
        var fake = new FakeDriveClient(new Dictionary<string, IReadOnlyList<DriveItem>>
        {
            ["root"] = new DriveItem[]
            {
                File("f1", "ok.pdf", Pdf),
                File("f2", "imagen.png", Png),
                File("f3", "planilla.xlsx", Xlsx)
            }
        });

        var service = BuildService(fake, mimeTypes: new[] { Pdf, Xlsx });

        var files = await CollectAsync(service.ListFilesUnderProjectAsync(1));

        Assert.Equal(new[] { "f1", "f3" }, files.Select(f => f.Id).ToArray());
    }

    [Fact]
    public async Task Si_MimeTypes_esta_vacio_no_emite_archivos()
    {
        var fake = new FakeDriveClient(new Dictionary<string, IReadOnlyList<DriveItem>>
        {
            ["root"] = new DriveItem[] { File("f1", "a.pdf", Pdf) }
        });

        var service = BuildService(fake, mimeTypes: Array.Empty<string>());

        var files = await CollectAsync(service.ListFilesUnderProjectAsync(1));

        Assert.Empty(files);
    }

    [Fact]
    public async Task Poda_carpetas_excluidas_por_nombre_exacto_y_no_descarga_su_subarbol()
    {
        var fake = new FakeDriveClient(new Dictionary<string, IReadOnlyList<DriveItem>>
        {
            ["root"] = new DriveItem[]
            {
                File("f1", "manual.pdf", Pdf),
                new DriveFolder("backup-id", "Backup")
            },
            ["backup-id"] = new DriveItem[] { File("f2", "viejo.pdf", Pdf) }
        });

        var service = BuildService(fake,
            mimeTypes: new[] { Pdf },
            exactNames: new[] { "Backup" });

        var files = await CollectAsync(service.ListFilesUnderProjectAsync(1));

        Assert.Equal(new[] { "f1" }, files.Select(f => f.Id).ToArray());
        Assert.DoesNotContain("backup-id", fake.VisitedFolders);
    }

    [Fact]
    public async Task Poda_carpetas_excluidas_por_patron_regex()
    {
        var fake = new FakeDriveClient(new Dictionary<string, IReadOnlyList<DriveItem>>
        {
            ["root"] = new DriveItem[]
            {
                File("f1", "manual.pdf", Pdf),
                new DriveFolder("arch-id", "archivo-2024")
            },
            ["arch-id"] = new DriveItem[] { File("f2", "viejo.pdf", Pdf) }
        });

        var service = BuildService(fake,
            mimeTypes: new[] { Pdf },
            regexPatterns: new[] { ".*archivo.*" });

        var files = await CollectAsync(service.ListFilesUnderProjectAsync(1));

        Assert.Equal(new[] { "f1" }, files.Select(f => f.Id).ToArray());
        Assert.DoesNotContain("arch-id", fake.VisitedFolders);
    }

    [Fact]
    public async Task Propaga_ProyectoDriveMappingNotFoundException_si_no_hay_mapeo()
    {
        var fake = new FakeDriveClient(new Dictionary<string, IReadOnlyList<DriveItem>>());
        var service = BuildService(fake, mimeTypes: new[] { Pdf });

        await Assert.ThrowsAsync<ProyectoDriveMappingNotFoundException>(async () =>
        {
            await CollectAsync(service.ListFilesUnderProjectAsync(999));
        });
    }

    private static DriveListingService BuildService(
        IDriveClient client,
        IEnumerable<string> mimeTypes,
        IEnumerable<string>? exactNames = null,
        IEnumerable<string>? regexPatterns = null)
    {
        var options = new DocumentAnalysisOptions
        {
            Drive = new DriveOptions
            {
                Mappings = new List<ProyectoFolderMapping>
                {
                    new() { ProyectoId = 1, FolderId = "root" }
                },
                MimeTypes = mimeTypes.ToList(),
                Exclusiones = new DriveExclusionOptions
                {
                    NombresExactos = (exactNames ?? Array.Empty<string>()).ToList(),
                    PatronesRegex  = (regexPatterns ?? Array.Empty<string>()).ToList()
                }
            }
        };

        var iOpts = Options.Create(options);
        var resolver = new OptionsBackedProyectoDriveResolver(iOpts);
        var policy = new DriveExclusionPolicy(iOpts);

        return new DriveListingService(resolver, client, policy, iOpts);
    }

    private static async Task<IReadOnlyList<DriveFile>> CollectAsync(
        IAsyncEnumerable<DriveFile> source)
    {
        var list = new List<DriveFile>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return list;
    }

    private static DriveFile File(string id, string name, string mimeType) =>
        new(
            Id: id,
            Name: name,
            MimeType: mimeType,
            WebViewLink: null,
            Size: 1024,
            CreatedTime: null,
            ModifiedTime: null);
}
