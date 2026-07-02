using ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;

namespace ISOAuditAgent.API.Tests;

// Tests de MetadataScanner.DetectarCodigo: el código del formulario se ancla al
// que está más cerca de "Vigencia", para no confundirlo con los códigos FR de la
// tabla del tailoring ("Código Documento Formal").
public class MetadataScannerTests
{
    [Fact]
    public void DetectarCodigo_PrefiereElCodigoCercaDeVigencia()
    {
        // Portada con "FR 99-05" + "Vigencia" adyacentes; la tabla, lejos, trae
        // muchos otros códigos FR. Debe elegir el de la portada.
        var bloque =
            "FR 99-05 Vigencia 02/01/2023 Tailoring del Proyecto Tipo B Fecha Responsable " +
            "Etapa Artefacto Responsable Codigo Documento Formal FR 29 FR 30 FR 31 FR 48";

        Assert.Equal("FR 99-05", MetadataScanner.DetectarCodigo(new[] { bloque }));
    }

    [Fact]
    public void DetectarCodigo_FallbackPrimerFr_SinVigencia()
    {
        Assert.Equal("FR 30-02", MetadataScanner.DetectarCodigo(new[] { "Algo aca FR 30-02 y mas texto" }));
    }

    [Fact]
    public void DetectarCodigo_NullCuandoNoHayCodigo()
    {
        Assert.Null(MetadataScanner.DetectarCodigo(new[] { "Documento sin codigo de formulario" }));
    }
}
