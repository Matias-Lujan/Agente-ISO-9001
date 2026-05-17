using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Tests.Parsing.Fixtures;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Parsing;

public sealed class DocxDocumentParserTests
{
    private const string DocxMime =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    [Fact]
    public void Soporta_MIME_de_Word()
    {
        var parser = new DocxDocumentParser();
        Assert.Contains(DocxMime, parser.SupportedMimeTypes);
    }

    [Fact]
    public async Task Parrafos_sin_encabezado_forman_una_sola_seccion_inicial()
    {
        var bytes = DocumentFixtures.BuildDocx(
            "Procedimiento de auditoría interna",
            "Versión 2.0",
            "Aprobado por: Gerente de Calidad");

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new DocxDocumentParser();

        var result = await parser.ParseAsync(stream, "procedimiento.docx");

        Assert.Equal(FormatoContenido.PlainText, result.Formato);
        Assert.Null(result.TextoNormalizado);
        var s = Assert.Single(result.Secciones);
        Assert.Equal("(Contenido inicial)", s.Titulo);
        Assert.True(s.TieneContenido);
    }

    [Fact]
    public async Task FR30_ERS_estilo_detecta_secciones_principales_y_TieneContenido()
    {
        var bytes = DocumentFixtures.BuildDocxWithStyledParagraphs(
            new List<(string?, string)>
            {
                ("Heading1", "Alcance"),
                (null, "El sistema cubre los procesos operativos de BDT."),
                ("Heading1", "Requisitos funcionales"),
                ("Heading1", "Anexos"),
                (null, "Anexo A: glosario de términos.")
            });

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new DocxDocumentParser();

        var result = await parser.ParseAsync(stream, "FR-30-ERS.docx");

        Assert.Null(result.TextoNormalizado);
        Assert.Equal(3, result.Secciones.Count);

        Assert.Equal("Alcance", result.Secciones[0].Titulo);
        Assert.True(result.Secciones[0].TieneContenido);

        Assert.Equal("Requisitos funcionales", result.Secciones[1].Titulo);
        Assert.False(result.Secciones[1].TieneContenido);

        Assert.Equal("Anexos", result.Secciones[2].Titulo);
        Assert.True(result.Secciones[2].TieneContenido);
    }

    [Fact]
    public async Task Hash_de_bytes_no_depende_del_parseo()
    {
        var bytes = DocumentFixtures.BuildDocx("Manual de Calidad ISO 9001:2015");

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new DocxDocumentParser();

        var result = await parser.ParseAsync(stream, "manual.docx");
        var hash = ContentHasher.ComputeSha256OfBytes(bytes);

        Assert.NotNull(result.Secciones);
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public async Task Titulos_de_heading_normalizan_NFC()
    {
        var bytes = DocumentFixtures.BuildDocxWithStyledParagraphs(
            new List<(string?, string)> { ("Heading1", "auditoría") });

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new DocxDocumentParser();

        var result = await parser.ParseAsync(stream, "x.docx");

        var titulo = Assert.Single(result.Secciones).Titulo;
        Assert.True(titulo.IsNormalized(System.Text.NormalizationForm.FormC));
        Assert.Contains("auditoría", titulo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DOCX_corrupto_envuelve_la_excepcion_con_nombre_archivo()
    {
        var basura = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0xFF, 0xFF, 0xFF, 0xFF };
        using var stream = DocumentFixtures.ToStream(basura);
        var parser = new DocxDocumentParser();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parser.ParseAsync(stream, "corrupto.docx"));

        Assert.Contains("corrupto.docx", ex.Message);
    }
}
