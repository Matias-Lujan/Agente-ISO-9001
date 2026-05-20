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
    public async Task Sin_outline_devuelve_seccion_agrupada_best_effort()
    {
        var bytes = DocumentFixtures.BuildPdf(
            "Manual de Calidad",
            "Procedimiento de auditoria interna ISO 9001");

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new PdfDocumentParser();

        var result = await parser.ParseAsync(stream, "manual.pdf");

        Assert.Equal(FormatoContenido.PlainText, result.Formato);
        Assert.Null(result.TextoNormalizado);
        var s = Assert.Single(result.Secciones);
        Assert.Contains(s.Titulo, new[] { "(Contenido inicial)", "(documento completo)" });
        Assert.True(s.TieneContenido);
    }

    [Fact]
    public async Task Hash_de_bytes_independiente_del_resultado_de_secciones()
    {
        var bytes = DocumentFixtures.BuildPdf(
            "Politica de Calidad",
            "Compromiso de la direccion con la mejora continua");

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new PdfDocumentParser();

        await parser.ParseAsync(stream, "politica.pdf");
        var hash = ContentHasher.ComputeSha256OfBytes(bytes);

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
