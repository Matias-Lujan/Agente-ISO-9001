using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Tailoring;
using ISOAuditAgent.DocumentAnalysis.Tests.Drive;
using ISOAuditAgent.DocumentAnalysis.Tests.Parsing.Fixtures;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Tailoring;

public sealed class TailoringReaderTests
{
    private const string Xlsx =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [Fact]
    public async Task ReadAsync_descarga_y_mapea_filas_del_FR29()
    {
        var bytes = DocumentFixtures.BuildXlsx(new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>
        {
            ["Principal"] = new List<IReadOnlyList<string>>
            {
                new[] { "Referencia", "Fase", "¿Aplica?", "Justificación", "Link Drive" },
                new[] { "FR-30", "Análisis y Diseño", "1", "", "https://docs.example.com/fr30" },
                new[] { "FR-48", "Seguimiento", "0", "No requerido en este release", "" }
            }
        });

        var tree = new Dictionary<string, IReadOnlyList<DriveItem>>
        {
            ["root"] =
            [
                new DriveFile(
                    "tail-1",
                    "FR-29 Tailoring Proyecto.xlsx",
                    Xlsx,
                    null,
                    bytes.Length,
                    null,
                    null)
            ]
        };

        var contents = new Dictionary<string, DriveFileContent>
        {
            ["tail-1"] = new DriveFileContent(
                "tail-1",
                "FR-29 Tailoring Proyecto.xlsx",
                Xlsx,
                bytes,
                bytes.Length,
                null)
        };

        var fakeClient = new FakeDriveClient(tree, contents);
        var iOpts = Options.Create(BuildOptions());
        ITailoringReader reader = new TailoringReader(
            new FakeDriveMcpClient(iOpts, fakeClient),
            new XlsxTableExtractor());

        var entradas = await reader.ReadAsync(1);

        Assert.Equal(2, entradas.Count);
        Assert.Equal("FR-30", entradas[0].CodigoArtefacto);
        Assert.True(entradas[0].Aplica);
        Assert.Equal("https://docs.example.com/fr30", entradas[0].UrlReferencia);

        Assert.Equal("FR-48", entradas[1].CodigoArtefacto);
        Assert.False(entradas[1].Aplica);
        Assert.Equal("No requerido en este release", entradas[1].JustificacionNoAplica);
    }

    [Fact]
    public async Task ReadAsync_sin_XLSX_FR29_lanza_TailoringWorkbookNotFoundException()
    {
        var tree = new Dictionary<string, IReadOnlyList<DriveItem>>
        {
            ["root"] =
            [
                new DriveFile("a", "notas.txt", "text/plain", null, 1, null, null)
            ]
        };
        var contents = new Dictionary<string, DriveFileContent>();

        var fakeClient = new FakeDriveClient(tree, contents);
        var iOpts = Options.Create(BuildOptions());
        var reader = new TailoringReader(
            new FakeDriveMcpClient(iOpts, fakeClient),
            new XlsxTableExtractor());

        await Assert.ThrowsAsync<TailoringWorkbookNotFoundException>(
            () => reader.ReadAsync(1));
    }

    private static DocumentAnalysisOptions BuildOptions() =>
        new()
        {
            Drive = new DriveOptions
            {
                Mappings = [new ProyectoFolderMapping { ProyectoId = 1, FolderId = "root" }],
                MimeTypes = [Xlsx],
                Exclusiones = new DriveExclusionOptions()
            }
        };
}
