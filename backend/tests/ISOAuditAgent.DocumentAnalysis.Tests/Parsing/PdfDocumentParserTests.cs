using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Tests.Parsing.Fixtures;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Parsing;

public sealed class PdfDocumentParserTests
{
    [Fact]
    public void Soporta_application_pdf()
    {
        var parser = new PdfDocumentParser();
        Assert.Contains("application/pdf", parser.SupportedMimeTypes);
    }

    [Fact]
    public async Task Extrae_texto_de_PDF_simple_y_devuelve_PlainText()
    {
        var bytes = DocumentFixtures.BuildPdf(
            "Manual de Calidad",
            "Procedimiento de auditoria interna ISO 9001");

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new PdfDocumentParser();

        var result = await parser.ParseAsync(stream, "manual.pdf");

        Assert.Equal(FormatoContenido.PlainText, result.Formato);
        Assert.Contains("Manual de Calidad", result.TextoNormalizado);
        Assert.Contains("auditoria interna ISO 9001", result.TextoNormalizado);
    }

    [Fact]
    public async Task ContenidoTextual_y_HashContenido_no_son_vacios_para_fixture_representativo()
    {
        var bytes = DocumentFixtures.BuildPdf(
            "Politica de Calidad",
            "Compromiso de la direccion con la mejora continua");

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new PdfDocumentParser();

        var result = await parser.ParseAsync(stream, "politica.pdf");
        var hash = ContentHasher.ComputeSha256OfBytes(bytes);

        Assert.False(string.IsNullOrWhiteSpace(result.TextoNormalizado));
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public async Task PDF_corrupto_envuelve_la_excepcion_con_nombre_archivo()
    {
        var basura = new byte[] { 0x42, 0x42, 0x42, 0x42, 0x42 };
        using var stream = DocumentFixtures.ToStream(basura);
        var parser = new PdfDocumentParser();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parser.ParseAsync(stream, "corrupto.pdf"));

        Assert.Contains("corrupto.pdf", ex.Message);
        Assert.NotNull(ex.InnerException);
    }
}
