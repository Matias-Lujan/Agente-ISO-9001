using System.Text;
using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Sources;
using ISOAuditAgent.DocumentAnalysis.Tests.Drive;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Sources;

/// <summary>
/// Tests de <see cref="DriveDocumentSource"/> (Fase 3) usando un
/// <see cref="FakeDriveClient"/> en memoria. Cubren:
/// <list type="bullet">
///   <item>Enumeración de documentos crudos para un proyecto.</item>
///   <item>Mapeo correcto de metadatos a <see cref="RawDocument"/>.</item>
///   <item>Stream con los bytes esperados, posicionado en 0.</item>
///   <item>Propagación de errores (mapeo faltante, descarga fallida).</item>
///   <item>Respeto del <see cref="CancellationToken"/>.</item>
/// </list>
/// </summary>
public sealed class DriveDocumentSourceTests
{
    private const string Pdf  = "application/pdf";
    private const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string Png  = "image/png";

    [Fact]
    public async Task Enumera_RawDocuments_descargando_cada_archivo_listado()
    {
        var bytesF1 = Encoding.UTF8.GetBytes("contenido-pdf-1");
        var bytesF2 = Encoding.UTF8.GetBytes("contenido-docx-2");
        var bytesF3 = Encoding.UTF8.GetBytes("contenido-xlsx-3");

        var modificado = new DateTimeOffset(2026, 4, 30, 10, 15, 0, TimeSpan.Zero);
        var creado     = new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);

        var fake = new FakeDriveClient(
            tree: new Dictionary<string, IReadOnlyList<DriveItem>>
            {
                ["root"] = new DriveItem[]
                {
                    File("f1", "manual.pdf", Pdf, "https://drive.google.com/f1", 1500, creado, modificado),
                    new DriveFolder("sub1", "Procesos")
                },
                ["sub1"] = new DriveItem[]
                {
                    File("f2", "registro.docx", Docx, "https://drive.google.com/f2", 2500, creado, modificado),
                    File("f3", "kpi.xlsx", Xlsx, "https://drive.google.com/f3", 3500, creado, modificado)
                }
            },
            contents: new Dictionary<string, DriveFileContent>
            {
                ["f1"] = Content("f1", "manual.pdf", Pdf, bytesF1, modificado),
                ["f2"] = Content("f2", "registro.docx", Docx, bytesF2, modificado),
                ["f3"] = Content("f3", "kpi.xlsx", Xlsx, bytesF3, modificado)
            });

        var source = BuildSource(fake, new[] { Pdf, Docx, Xlsx });

        var raws = await CollectAsync(source.EnumerateAsync(1));

        Assert.Equal(3, raws.Count);
        Assert.All(raws, r => Assert.Equal(FuenteDocumento.GoogleDrive, r.Fuente));
        Assert.Equal(new[] { "f1", "f2", "f3" }, raws.Select(r => r.IdEnFuente).ToArray());
        Assert.Equal(new[] { "manual.pdf", "registro.docx", "kpi.xlsx" }, raws.Select(r => r.NombreArchivo).ToArray());
        Assert.Equal(new[] { Pdf, Docx, Xlsx }, raws.Select(r => r.MimeType).ToArray());
        Assert.All(raws, r => Assert.Equal(creado, r.FechaCreacion));
        Assert.All(raws, r => Assert.Equal(modificado, r.FechaModificacion));
        Assert.Equal(new[] { "https://drive.google.com/f1", "https://drive.google.com/f2", "https://drive.google.com/f3" },
                     raws.Select(r => r.UrlReferencia).ToArray());

        var leidos = raws.Select(ReadAllBytes).ToArray();
        Assert.Equal(bytesF1, leidos[0]);
        Assert.Equal(bytesF2, leidos[1]);
        Assert.Equal(bytesF3, leidos[2]);

        foreach (var r in raws) r.Dispose();
    }

    [Fact]
    public async Task Filtra_archivos_no_permitidos_antes_de_descargar()
    {
        var fake = new FakeDriveClient(
            tree: new Dictionary<string, IReadOnlyList<DriveItem>>
            {
                ["root"] = new DriveItem[]
                {
                    File("f1", "ok.pdf", Pdf, null, 100, null, null),
                    File("f2", "imagen.png", Png, null, 100, null, null)
                }
            },
            contents: new Dictionary<string, DriveFileContent>
            {
                ["f1"] = Content("f1", "ok.pdf", Pdf, new byte[] { 1, 2, 3 }, null)
            });

        var source = BuildSource(fake, new[] { Pdf });

        var raws = await CollectAsync(source.EnumerateAsync(1));

        Assert.Single(raws);
        Assert.Equal("f1", raws[0].IdEnFuente);
        raws[0].Dispose();
    }

    [Fact]
    public async Task Stream_arranca_en_position_cero_y_es_legible()
    {
        var bytes = Encoding.UTF8.GetBytes("hola-mundo");
        var fake = new FakeDriveClient(
            tree: new Dictionary<string, IReadOnlyList<DriveItem>>
            {
                ["root"] = new DriveItem[]
                {
                    File("f1", "x.pdf", Pdf, null, bytes.Length, null, null)
                }
            },
            contents: new Dictionary<string, DriveFileContent>
            {
                ["f1"] = Content("f1", "x.pdf", Pdf, bytes, null)
            });

        var source = BuildSource(fake, new[] { Pdf });

        await foreach (var raw in source.EnumerateAsync(1))
        {
            using (raw)
            {
                Assert.Equal(0, raw.Contenido.Position);
                Assert.True(raw.Contenido.CanRead);

                using var sr = new StreamReader(raw.Contenido, Encoding.UTF8, leaveOpen: true);
                var leido = await sr.ReadToEndAsync();
                Assert.Equal("hola-mundo", leido);
            }
        }
    }

    [Fact]
    public async Task Propaga_ProyectoDriveMappingNotFoundException_si_no_hay_mapeo()
    {
        var fake = new FakeDriveClient(new Dictionary<string, IReadOnlyList<DriveItem>>());
        var source = BuildSource(fake, new[] { Pdf });

        await Assert.ThrowsAsync<ProyectoDriveMappingNotFoundException>(async () =>
        {
            await CollectAsync(source.EnumerateAsync(999));
        });
    }

    [Fact]
    public async Task Propaga_excepcion_si_falla_descarga_de_un_archivo()
    {
        var fake = new FakeDriveClient(
            tree: new Dictionary<string, IReadOnlyList<DriveItem>>
            {
                ["root"] = new DriveItem[]
                {
                    File("f1", "manual.pdf", Pdf, null, 100, null, null)
                }
            });

        var source = BuildSource(fake, new[] { Pdf });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await CollectAsync(source.EnumerateAsync(1));
        });
    }

    [Fact]
    public async Task Respeta_CancellationToken()
    {
        var fake = new FakeDriveClient(
            tree: new Dictionary<string, IReadOnlyList<DriveItem>>
            {
                ["root"] = new DriveItem[]
                {
                    File("f1", "a.pdf", Pdf, null, 100, null, null),
                    File("f2", "b.pdf", Pdf, null, 100, null, null)
                }
            },
            contents: new Dictionary<string, DriveFileContent>
            {
                ["f1"] = Content("f1", "a.pdf", Pdf, new byte[] { 1 }, null),
                ["f2"] = Content("f2", "b.pdf", Pdf, new byte[] { 2 }, null)
            });

        var source = BuildSource(fake, new[] { Pdf });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await CollectAsync(source.EnumerateAsync(1, cts.Token));
        });
    }

    [Fact]
    public void Fuente_es_GoogleDrive()
    {
        var source = BuildSource(
            new FakeDriveClient(new Dictionary<string, IReadOnlyList<DriveItem>>()),
            new[] { Pdf });

        Assert.Equal(FuenteDocumento.GoogleDrive, source.Fuente);
    }

    private static DriveDocumentSource BuildSource(IDriveClient client, IEnumerable<string> mimeTypes)
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
                Exclusiones = new DriveExclusionOptions()
            }
        };

        var iOpts = Options.Create(options);
        var resolver = new OptionsBackedProyectoDriveResolver(iOpts);
        var policy = new DriveExclusionPolicy(iOpts);
        var listing = new DriveListingService(resolver, client, policy, iOpts);

        return new DriveDocumentSource(listing, client);
    }

    private static async Task<List<RawDocument>> CollectAsync(
        IAsyncEnumerable<RawDocument> source)
    {
        var list = new List<RawDocument>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return list;
    }

    private static byte[] ReadAllBytes(RawDocument raw)
    {
        using var ms = new MemoryStream();
        raw.Contenido.CopyTo(ms);
        return ms.ToArray();
    }

    private static DriveFile File(
        string id, string name, string mime, string? webViewLink,
        long? size, DateTimeOffset? created, DateTimeOffset? modified) =>
        new(
            Id: id,
            Name: name,
            MimeType: mime,
            WebViewLink: webViewLink,
            Size: size,
            CreatedTime: created,
            ModifiedTime: modified);

    private static DriveFileContent Content(
        string id, string name, string mime, byte[] bytes, DateTimeOffset? modified) =>
        new(
            Id: id,
            Name: name,
            MimeType: mime,
            Bytes: bytes,
            Size: bytes.LongLength,
            ModifiedTime: modified);
}
