using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Tests.Parsing.Fixtures;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Parsing;

public sealed class XlsxTableExtractorTests
{
    [Fact]
    public void NormalizeHeaderKey_quita_tildes_y_minusculas()
    {
        Assert.Equal("codigo etapa", XlsxTableExtractor.NormalizeHeaderKey("  Código   Etapa  "));
    }

    [Fact]
    public void Detecta_fila_de_encabezados_aunque_la_primera_fila_sea_titulo()
    {
        var bytes = DocumentFixtures.BuildXlsx(new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>
        {
            ["Hoja1"] = new List<IReadOnlyList<string>>
            {
                new[] { "Tailoring — proyecto demo" },
                new[] { "Código", "Etapa", "Aplica", "URL" },
                new[] { "FR-30", "Análisis", "si", "https://example.com/a" },
                new[] { "FR-32", "Testing", "no", "https://example.com/b" }
            }
        });

        using var stream = DocumentFixtures.ToStream(bytes);
        var ex = new XlsxTableExtractor();

        var (_, _, data) = ex.ExtractKeyedTable(stream, "tailoring.xlsx");

        Assert.Equal(2, data.Count);
        Assert.Equal("FR-30", data[0][TailoringHeaderKey("codigo")]);
        Assert.Equal("https://example.com/a", data[0][TailoringHeaderKey("url")]);
    }

    private static string TailoringHeaderKey(string logical)
    {
        return logical switch
        {
            "codigo" => XlsxTableExtractor.NormalizeHeaderKey("Código"),
            "url" => XlsxTableExtractor.NormalizeHeaderKey("URL"),
            _ => XlsxTableExtractor.NormalizeHeaderKey(logical)
        };
    }
}
