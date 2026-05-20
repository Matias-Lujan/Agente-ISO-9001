using ISOAuditAgent.DocumentAnalysis.Agent.Tools;
using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Tests.Drive;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Agent;

public sealed class VerificarArtefactoToolTests
{
    private const string Pdf  = "application/pdf";
    private const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string Plain = "text/plain";

    [Fact]
    public async Task Sin_URL_busca_por_templateDriveFilename_exacto_en_carpeta_proyecto()
    {
        // proyecto: tiene FR-30.docx con MIME no soportado para evitar parseo real.
        // El test verifica solo la resolución del archivo, no el parser.
        var fileId = "doc-fr30";
        var tree = new Dictionary<string, IReadOnlyList<DriveItem>>
        {
            ["proyecto-fld"] = new DriveItem[]
            {
                new DriveFile(fileId, "FR-30.docx", Plain, null, 10, null, null)
            }
        };
        var contents = new Dictionary<string, DriveFileContent>
        {
            [fileId] = new(fileId, "FR-30.docx", Plain, new byte[] { 0x01 }, 1, null)
        };

        var (tool, _) = BuildTool(tree, contents,
            proyectoFolderId: "proyecto-fld",
            allowedMimes: new[] { Pdf, Docx, Plain });

        var v = await tool.VerificarArtefactoAsync(
            proyectoId: 1,
            artefactoEsperadoId: 30,
            urlReferencia: null,
            codigoArtefacto: "FR 30",
            templateDriveFilename: "FR-30.docx",
            driveFolderIdTemplates: null);

        Assert.Equal("FR-30.docx", v.NombreArchivo);
        Assert.False(v.Encontrado);
        Assert.Contains("MIME", v.ErrorMensaje ?? string.Empty);
    }

    [Fact]
    public async Task Sin_match_por_nombre_busca_por_codigo_del_artefacto()
    {
        // El proyecto guarda el archivo con un nombre distinto al template
        // pero conserva el código FR 30 en el nombre.
        var fileId = "doc-real";
        var tree = new Dictionary<string, IReadOnlyList<DriveItem>>
        {
            ["proyecto-fld"] = new DriveItem[]
            {
                new DriveFile(fileId, "ERS - FR 30 - Proyecto X.docx", Plain, null, 10, null, null),
                new DriveFile("otro", "Notas.docx", Plain, null, 10, null, null)
            }
        };
        var contents = new Dictionary<string, DriveFileContent>
        {
            [fileId] = new(fileId, "ERS - FR 30 - Proyecto X.docx", Plain, new byte[] { 0x01 }, 1, null)
        };

        var (tool, _) = BuildTool(tree, contents,
            proyectoFolderId: "proyecto-fld",
            allowedMimes: new[] { Pdf, Docx, Plain });

        var v = await tool.VerificarArtefactoAsync(
            proyectoId: 1,
            artefactoEsperadoId: 30,
            urlReferencia: null,
            codigoArtefacto: "FR 30",
            templateDriveFilename: "FR-30.docx",
            driveFolderIdTemplates: null);

        Assert.Equal("ERS - FR 30 - Proyecto X.docx", v.NombreArchivo);
    }

    [Fact]
    public async Task Resuelve_TemplateFileId_desde_carpeta_templates()
    {
        var templateFileId = "tpl-fr30";
        var tree = new Dictionary<string, IReadOnlyList<DriveItem>>
        {
            ["proyecto-fld"] = Array.Empty<DriveItem>(),
            ["templates-fld"] = new DriveItem[]
            {
                new DriveFile(templateFileId, "FR-30.docx", Plain, null, 10, null, null),
                new DriveFile("tpl-fr32", "FR-32.docx", Plain, null, 10, null, null)
            }
        };

        var (tool, _) = BuildTool(tree, contents: null,
            proyectoFolderId: "proyecto-fld",
            allowedMimes: new[] { Pdf, Docx, Plain });

        var v = await tool.VerificarArtefactoAsync(
            proyectoId: 1,
            artefactoEsperadoId: 30,
            urlReferencia: null,
            codigoArtefacto: "FR 30",
            templateDriveFilename: "FR-30.docx",
            driveFolderIdTemplates: "templates-fld");

        Assert.Equal(templateFileId, v.TemplateFileIdResuelto);
    }

    [Fact]
    public async Task Sin_URL_ni_nombre_ni_codigo_devuelve_NoLocalizado()
    {
        var tree = new Dictionary<string, IReadOnlyList<DriveItem>>
        {
            ["proyecto-fld"] = Array.Empty<DriveItem>()
        };

        var (tool, _) = BuildTool(tree, contents: null,
            proyectoFolderId: "proyecto-fld",
            allowedMimes: new[] { Pdf, Docx, Plain });

        var v = await tool.VerificarArtefactoAsync(
            proyectoId: 1,
            artefactoEsperadoId: 99,
            urlReferencia: null,
            codigoArtefacto: null,
            templateDriveFilename: null,
            driveFolderIdTemplates: null);

        Assert.False(v.Encontrado);
        Assert.Null(v.NombreArchivo);
        Assert.Null(v.TemplateFileIdResuelto);
    }

    private static (VerificarArtefactoTool Tool, FakeDriveClient Fake) BuildTool(
        Dictionary<string, IReadOnlyList<DriveItem>> tree,
        Dictionary<string, DriveFileContent>? contents,
        string proyectoFolderId,
        IEnumerable<string> allowedMimes)
    {
        var fake = new FakeDriveClient(tree, contents);

        var opts = Options.Create(new DocumentAnalysisOptions
        {
            Drive = new DriveOptions
            {
                Mappings = [new ProyectoFolderMapping { ProyectoId = 1, FolderId = proyectoFolderId }],
                MimeTypes = allowedMimes.ToList(),
                Exclusiones = new DriveExclusionOptions()
            }
        });

        var mcp = new FakeDriveMcpClient(opts, fake);
        var parsers = new DocumentParserRegistry(
            [new PdfDocumentParser(), new DocxDocumentParser(), new XlsxDocumentParser()]);
        var tool = new VerificarArtefactoTool(mcp, parsers, NullLogger<VerificarArtefactoTool>.Instance);
        return (tool, fake);
    }
}
