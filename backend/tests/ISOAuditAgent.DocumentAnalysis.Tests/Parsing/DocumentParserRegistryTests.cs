using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Parsing;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Parsing;

public sealed class DocumentParserRegistryTests
{
    [Fact]
    public void Construye_indice_con_un_parser_por_MIME()
    {
        var registry = new DocumentParserRegistry(new IDocumentParser[]
        {
            new FakeParser("application/pdf"),
            new FakeParser("application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        });

        Assert.Equal(2, registry.SupportedMimeTypes.Count);
    }

    [Fact]
    public void TryGetParser_devuelve_true_para_MIME_registrado()
    {
        var pdf = new FakeParser("application/pdf");
        var registry = new DocumentParserRegistry(new IDocumentParser[] { pdf });

        Assert.True(registry.TryGetParser("application/pdf", out var found));
        Assert.Same(pdf, found);
    }

    [Fact]
    public void TryGetParser_devuelve_false_para_MIME_no_registrado()
    {
        var registry = new DocumentParserRegistry(new IDocumentParser[]
        {
            new FakeParser("application/pdf")
        });

        Assert.False(registry.TryGetParser("image/png", out var found));
        Assert.Null(found);
    }

    [Fact]
    public void GetRequiredParser_lanza_NotSupported_para_MIME_no_registrado()
    {
        var registry = new DocumentParserRegistry(Array.Empty<IDocumentParser>());

        var ex = Assert.Throws<NotSupportedException>(
            () => registry.GetRequiredParser("application/pdf"));

        Assert.Contains("application/pdf", ex.Message);
    }

    [Fact]
    public void Lanza_si_dos_parsers_se_registran_para_el_mismo_MIME()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new DocumentParserRegistry(new IDocumentParser[]
            {
                new FakeParser("application/pdf"),
                new FakeParser("application/pdf")
            }));

        Assert.Contains("application/pdf", ex.Message);
        Assert.Contains("Conflicto", ex.Message);
    }

    [Fact]
    public void TryGetParser_es_case_insensitive_en_el_MIME()
    {
        var registry = new DocumentParserRegistry(new IDocumentParser[]
        {
            new FakeParser("application/pdf")
        });

        Assert.True(registry.TryGetParser("Application/PDF", out _));
    }

    private sealed class FakeParser : IDocumentParser
    {
        public FakeParser(params string[] mimes) => SupportedMimeTypes = mimes;

        public IReadOnlyCollection<string> SupportedMimeTypes { get; }

        public Task<DocumentParseResult> ParseAsync(
            Stream contenido,
            string nombreArchivo,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new DocumentParseResult(
                FormatoContenido.PlainText,
                new[] { new SeccionDetectada("fake", true) },
                null));
    }
}
