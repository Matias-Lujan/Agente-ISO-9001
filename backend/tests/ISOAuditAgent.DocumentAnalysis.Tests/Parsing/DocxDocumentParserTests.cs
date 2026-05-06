using ISOAuditAgent.Contracts;
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
    public async Task Extrae_texto_de_DOCX_y_devuelve_PlainText()
    {
        var bytes = DocumentFixtures.BuildDocx(
            "Procedimiento de auditoría interna",
            "Versión 2.0",
            "Aprobado por: Gerente de Calidad");

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new DocxDocumentParser();

        var result = await parser.ParseAsync(stream, "procedimiento.docx");

        Assert.Equal(FormatoContenido.PlainText, result.Formato);
        Assert.Contains("Procedimiento de auditoría interna", result.TextoNormalizado);
        Assert.Contains("Versión 2.0", result.TextoNormalizado);
        Assert.Contains("Aprobado por: Gerente de Calidad", result.TextoNormalizado);
    }

    [Fact]
    public async Task ContenidoTextual_y_HashContenido_no_son_vacios_para_fixture_representativo()
    {
        var bytes = DocumentFixtures.BuildDocx("Manual de Calidad ISO 9001:2015");

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new DocxDocumentParser();

        var result = await parser.ParseAsync(stream, "manual.docx");
        var hash = ContentHasher.ComputeSha256OfBytes(bytes);

        Assert.False(string.IsNullOrWhiteSpace(result.TextoNormalizado));
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public async Task Texto_normalizado_aplica_NFC_a_acentos()
    {
        var bytes = DocumentFixtures.BuildDocx("auditoría");
        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new DocxDocumentParser();

        var result = await parser.ParseAsync(stream, "x.docx");

        Assert.True(result.TextoNormalizado.IsNormalized(System.Text.NormalizationForm.FormC));
        Assert.Contains("auditoría", result.TextoNormalizado);
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
