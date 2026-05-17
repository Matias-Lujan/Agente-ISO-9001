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
    public async Task Cada_hoja_es_seccion_TieneContenido_cuando_mas_de_una_fila()
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
        Assert.Null(result.TextoNormalizado);
        var s = Assert.Single(result.Secciones);
        Assert.Equal("indicadores", s.Titulo, StringComparer.OrdinalIgnoreCase);
        Assert.True(s.TieneContenido);
    }

    [Fact]
    public async Task Multiples_hojas_generan_una_seccion_por_hoja()
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

        Assert.Equal(2, result.Secciones.Count);
        Assert.Equal("resumen", result.Secciones[0].Titulo, StringComparer.OrdinalIgnoreCase);
        Assert.False(result.Secciones[0].TieneContenido);
        Assert.Equal("detalle", result.Secciones[1].Titulo, StringComparer.OrdinalIgnoreCase);
        Assert.True(result.Secciones[1].TieneContenido);
    }

    [Fact]
    public async Task Hoja_con_solo_encabezado_marca_TieneContenido_falso()
    {
        var bytes = DocumentFixtures.BuildXlsx(new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>
        {
            ["Hoja1"] = new List<IReadOnlyList<string>>
            {
                new[] { "a|b", "c" }
            }
        });

        using var stream = DocumentFixtures.ToStream(bytes);
        var parser = new XlsxDocumentParser();

        var result = await parser.ParseAsync(stream, "x.xlsx");

        Assert.False(Assert.Single(result.Secciones).TieneContenido);
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
