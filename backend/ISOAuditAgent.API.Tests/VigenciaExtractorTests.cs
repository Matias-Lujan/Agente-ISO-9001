using ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;

namespace ISOAuditAgent.API.Tests;

// ============================================================================
//  Tests de extracción de la Vigencia (etiqueta "Vigencia" + dd/mm/yyyy) sobre
//  los FR reales de BDT. Confirma contra los dos casos estructurales distintos:
//   - FR 48: la vigencia vive en el HEADER → 01/07/2021.
//   - FR 30: la vigencia vive en el FOOTER → 13/08/2020. El cuerpo del FR 30
//     tiene OTRA fecha (08/03/2024, del historial de cambios): anclar a la
//     etiqueta "Vigencia" evita devolver esa por error.
// ============================================================================
public class VigenciaExtractorTests
{
    private static DateOnly? Extraer(string fixture)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture);
        var bytes = File.ReadAllBytes(path);
        var bloques = new DocxTemplateParser().ExtraerBloquesVigencia(bytes);
        return VigenciaScanner.Detectar(bloques);
    }

    [Fact]
    public void FR48_Doc_VigenciaDesdeHeader()
        => Assert.Equal(new DateOnly(2021, 7, 1), Extraer("FR48_doc.docx"));

    [Fact]
    public void FR48_Template_Vigencia()
        => Assert.Equal(new DateOnly(2021, 7, 1), Extraer("FR48_template.docx"));

    [Fact]
    public void FR30_Doc_VigenciaDesdeFooter_NoLaFechaDelCuerpo()
    {
        var vigencia = Extraer("FR30_doc.docx");
        Assert.Equal(new DateOnly(2020, 8, 13), vigencia);
        // Trampa: el cuerpo trae 08/03/2024 (historial de cambios). No es la vigencia.
        Assert.NotEqual(new DateOnly(2024, 3, 8), vigencia);
    }

    [Fact]
    public void FR30_Template_Vigencia()
        => Assert.Equal(new DateOnly(2020, 8, 13), Extraer("FR30_template.docx"));
}
