using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Tests.Parsing.Fixtures;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Parsing;

public sealed class XlsxDocumentParserTests
{
    private const string XlsxMime =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [Fact]
    public void Soporta_MIME_de_Excel()
    {
        var parser = new XlsxDocumentParser();
        Assert.Contains(XlsxMime, parser.SupportedMimeTypes);
    }

    [Fact]
    public async Task Excel_se_devuelve_como_Markdown()
    {
        var bytes = DocumentFixtures.BuildXlsx(new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>
        {
            ["Indicadores"] = new List<IReadOnlyList<string>>
            {
                new[] { "Indicador", "Meta", "Resultado" },
                new[] { "Satisfacción cliente", "90", "92" },
                new[] { "Tiempo respuesta", "48hs", "36hs" }
            }
        });

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new XlsxDocumentParser();

        var result = await parser.ParseAsync(stream, "kpis.xlsx");

        Assert.Equal(FormatoContenido.Markdown, result.Formato);
        Assert.Contains("## Indicadores", result.TextoNormalizado);
        Assert.Contains("| Indicador | Meta | Resultado |", result.TextoNormalizado);
        Assert.Contains("|---|---|---|", result.TextoNormalizado);
        Assert.Contains("| Satisfacción cliente | 90 | 92 |", result.TextoNormalizado);
        Assert.Contains("| Tiempo respuesta | 48hs | 36hs |", result.TextoNormalizado);
    }

    [Fact]
    public async Task Multiples_hojas_generan_secciones_separadas_por_encabezado()
    {
        var bytes = DocumentFixtures.BuildXlsx(new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>
        {
            ["Resumen"] = new List<IReadOnlyList<string>>
            {
                new[] { "Total auditorias", "12" }
            },
            ["Detalle"] = new List<IReadOnlyList<string>>
            {
                new[] { "Fecha", "Auditor" },
                new[] { "2026-04-01", "JP" }
            }
        });

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new XlsxDocumentParser();

        var result = await parser.ParseAsync(stream, "x.xlsx");

        Assert.Contains("## Resumen", result.TextoNormalizado);
        Assert.Contains("## Detalle", result.TextoNormalizado);
        Assert.Contains("| Fecha | Auditor |", result.TextoNormalizado);
        Assert.Contains("| 2026-04-01 | JP |", result.TextoNormalizado);
    }

    [Fact]
    public async Task ContenidoTextual_y_HashContenido_no_son_vacios_para_fixture_representativo()
    {
        var bytes = DocumentFixtures.BuildXlsx(new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>
        {
            ["Hoja1"] = new List<IReadOnlyList<string>>
            {
                new[] { "a", "b" },
                new[] { "1", "2" }
            }
        });

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new XlsxDocumentParser();

        var result = await parser.ParseAsync(stream, "x.xlsx");
        var hash = ContentHasher.ComputeSha256OfBytes(bytes);

        Assert.False(string.IsNullOrWhiteSpace(result.TextoNormalizado));
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public async Task Escapa_pipes_en_celdas_para_no_romper_la_tabla_Markdown()
    {
        var bytes = DocumentFixtures.BuildXlsx(new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>
        {
            ["x"] = new List<IReadOnlyList<string>>
            {
                new[] { "a|b", "c" }
            }
        });

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new XlsxDocumentParser();

        var result = await parser.ParseAsync(stream, "x.xlsx");

        Assert.Contains(@"a\|b", result.TextoNormalizado);
    }

    [Fact]
    public async Task XLSX_corrupto_envuelve_la_excepcion_con_nombre_archivo()
    {
        var basura = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00 };
        using var stream = DocumentFixtures.ToStream(basura);
        var parser = new XlsxDocumentParser();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => parser.ParseAsync(stream, "corrupto.xlsx"));

        Assert.Contains("corrupto.xlsx", ex.Message);
    }
}
