using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;
using ComplianceParser = ISOAuditAgent.API.Agents.ComplianceValidation.ComplianceHallazgosPreliminaresParser;
using ConsistencyParser = ISOAuditAgent.API.Agents.ConsistencyVerification.HallazgosPreliminaresParser;

namespace ISOAuditAgent.API.Tests.Agents;

// Caracterización de los dos parsers de hallazgos preliminares del LLM
// (ComplianceValidation y ConsistencyVerification). Congela el comportamiento
// actual ANTES de deduplicar el esqueleto común: happy path, dedup, forzado de
// OrigenRegla y cada camino que debe lanzar excepción. Son funciones puras
// (internal static, accesibles por InternalsVisibleTo).
public class HallazgosPreliminaresParsersTests
{
    // ======================= ComplianceValidation =======================

    [Fact]
    public void Compliance_HappyPath_FuerzaTailoring()
    {
        var json = """
            {"hallazgos":[
              {"artefactoEsperadoId":1,"descripcion":"Incoherencia URL","justificacion":"La URL del tailoring no coincide con el archivo.","origenRegla":"Tailoring"}
            ]}
            """;

        var r = ComplianceParser.Parsear(json, new HashSet<int> { 1 });

        var h = Assert.Single(r);
        Assert.Equal(1, h.ArtefactoEsperadoId);
        Assert.Equal("Incoherencia URL", h.Descripcion);
        Assert.Equal("La URL del tailoring no coincide con el archivo.", h.Justificacion);
        Assert.Equal(OrigenRegla.Tailoring, h.OrigenRegla);
    }

    [Fact]
    public void Compliance_TailoringCaseInsensitive_Ok()
    {
        var json = """{"hallazgos":[{"artefactoEsperadoId":1,"descripcion":"d","justificacion":"j","origenRegla":"tAiLoRiNg"}]}""";

        var h = Assert.Single(ComplianceParser.Parsear(json, new HashSet<int> { 1 }));
        Assert.Equal(OrigenRegla.Tailoring, h.OrigenRegla);
    }

    [Fact]
    public void Compliance_NoDeduplica()
    {
        // Dos hallazgos idénticos: Compliance NO deduplica (a diferencia de Consistency).
        var json = """
            {"hallazgos":[
              {"artefactoEsperadoId":1,"descripcion":"d","justificacion":"j","origenRegla":"Tailoring"},
              {"artefactoEsperadoId":1,"descripcion":"d","justificacion":"j","origenRegla":"Tailoring"}
            ]}
            """;

        Assert.Equal(2, ComplianceParser.Parsear(json, new HashSet<int> { 1 }).Count);
    }

    [Fact]
    public void Compliance_ListaVacia_Ok()
    {
        Assert.Empty(ComplianceParser.Parsear("""{"hallazgos":[]}""", new HashSet<int>()));
    }

    [Theory]
    [InlineData("esto no es json")]
    [InlineData("[]")]                                  // raíz no es objeto
    [InlineData("{}")]                                  // falta 'hallazgos'
    [InlineData("""{"hallazgos": 5}""")]                // 'hallazgos' no es array
    [InlineData("""{"hallazgos":[1]}""")]               // item no es objeto
    [InlineData("""{"hallazgos":[{"descripcion":"d","justificacion":"j","origenRegla":"Tailoring"}]}""")] // falta id
    public void Compliance_JsonMalformado_Lanza(string json)
    {
        Assert.Throws<InvalidOperationException>(
            () => ComplianceParser.Parsear(json, new HashSet<int> { 1 }));
    }

    [Fact]
    public void Compliance_IdNoExpuesto_Lanza()
    {
        var json = """{"hallazgos":[{"artefactoEsperadoId":99,"descripcion":"d","justificacion":"j","origenRegla":"Tailoring"}]}""";
        Assert.Throws<InvalidOperationException>(
            () => ComplianceParser.Parsear(json, new HashSet<int> { 1 }));
    }

    [Theory]
    [InlineData("")]      // descripción vacía
    [InlineData("   ")]   // descripción en blanco
    public void Compliance_DescripcionVacia_Lanza(string desc)
    {
        var json = $$"""{"hallazgos":[{"artefactoEsperadoId":1,"descripcion":"{{desc}}","justificacion":"j","origenRegla":"Tailoring"}]}""";
        Assert.Throws<InvalidOperationException>(
            () => ComplianceParser.Parsear(json, new HashSet<int> { 1 }));
    }

    [Fact]
    public void Compliance_JustificacionVacia_Lanza()
    {
        var json = """{"hallazgos":[{"artefactoEsperadoId":1,"descripcion":"d","justificacion":"  ","origenRegla":"Tailoring"}]}""";
        Assert.Throws<InvalidOperationException>(
            () => ComplianceParser.Parsear(json, new HashSet<int> { 1 }));
    }

    [Theory]
    [InlineData("Procedimiento")]
    [InlineData("Template")]
    [InlineData("")]
    public void Compliance_OrigenNoTailoring_Lanza(string origen)
    {
        var json = $$"""{"hallazgos":[{"artefactoEsperadoId":1,"descripcion":"d","justificacion":"j","origenRegla":"{{origen}}"}]}""";
        Assert.Throws<InvalidOperationException>(
            () => ComplianceParser.Parsear(json, new HashSet<int> { 1 }));
    }

    // ======================= ConsistencyVerification =======================

    [Fact]
    public void Consistency_HappyPath_ParseaTresOrigenes()
    {
        var json = """
            {"hallazgos":[
              {"artefactoEsperadoId":1,"descripcion":"d1","justificacion":"j1","origenRegla":"Procedimiento"},
              {"artefactoEsperadoId":2,"descripcion":"d2","justificacion":"j2","origenRegla":"Template"},
              {"artefactoEsperadoId":2,"descripcion":"d3","justificacion":"j3","origenRegla":"tailoring"}
            ]}
            """;

        var r = ConsistencyParser.Parsear(json, new HashSet<int> { 1, 2 }, auditoriaId: 99);

        Assert.Equal(99, r.AuditoriaId);
        Assert.Equal(AgenteOrigen.ConsistencyVerification, r.AgenteOrigen);
        Assert.Equal(3, r.Hallazgos.Count);
        Assert.Equal(OrigenRegla.Procedimiento, r.Hallazgos[0].OrigenRegla);
        Assert.Equal(OrigenRegla.Template, r.Hallazgos[1].OrigenRegla);
        Assert.Equal(OrigenRegla.Tailoring, r.Hallazgos[2].OrigenRegla);
    }

    [Fact]
    public void Consistency_Deduplica()
    {
        // Dos hallazgos idénticos (mismo id+descripcion+justificacion) → uno solo.
        var json = """
            {"hallazgos":[
              {"artefactoEsperadoId":1,"descripcion":"d","justificacion":"j","origenRegla":"Template"},
              {"artefactoEsperadoId":1,"descripcion":"d","justificacion":"j","origenRegla":"Template"}
            ]}
            """;

        Assert.Single(ConsistencyParser.Parsear(json, new HashSet<int> { 1 }, 1).Hallazgos);
    }

    [Fact]
    public void Consistency_ListaVacia_Ok()
    {
        var r = ConsistencyParser.Parsear("""{"hallazgos":[]}""", new HashSet<int>(), 7);
        Assert.Empty(r.Hallazgos);
        Assert.Equal(7, r.AuditoriaId);
        Assert.Equal(AgenteOrigen.ConsistencyVerification, r.AgenteOrigen);
    }

    [Theory]
    [InlineData("esto no es json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"hallazgos": 5}""")]
    [InlineData("""{"hallazgos":[1]}""")]
    [InlineData("""{"hallazgos":[{"descripcion":"d","justificacion":"j","origenRegla":"Template"}]}""")]
    public void Consistency_JsonMalformado_Lanza(string json)
    {
        Assert.Throws<InvalidOperationException>(
            () => ConsistencyParser.Parsear(json, new HashSet<int> { 1 }, 1));
    }

    [Fact]
    public void Consistency_IdNoExpuesto_Lanza()
    {
        var json = """{"hallazgos":[{"artefactoEsperadoId":99,"descripcion":"d","justificacion":"j","origenRegla":"Template"}]}""";
        Assert.Throws<InvalidOperationException>(
            () => ConsistencyParser.Parsear(json, new HashSet<int> { 1 }, 1));
    }

    [Fact]
    public void Consistency_CamposVacios_Lanzan()
    {
        var sinDesc = """{"hallazgos":[{"artefactoEsperadoId":1,"descripcion":"  ","justificacion":"j","origenRegla":"Template"}]}""";
        var sinJust = """{"hallazgos":[{"artefactoEsperadoId":1,"descripcion":"d","justificacion":"","origenRegla":"Template"}]}""";
        Assert.Throws<InvalidOperationException>(() => ConsistencyParser.Parsear(sinDesc, new HashSet<int> { 1 }, 1));
        Assert.Throws<InvalidOperationException>(() => ConsistencyParser.Parsear(sinJust, new HashSet<int> { 1 }, 1));
    }

    [Theory]
    [InlineData("Otra")]
    [InlineData("")]
    [InlineData("NC")]
    public void Consistency_OrigenInvalido_Lanza(string origen)
    {
        var json = $$"""{"hallazgos":[{"artefactoEsperadoId":1,"descripcion":"d","justificacion":"j","origenRegla":"{{origen}}"}]}""";
        Assert.Throws<InvalidOperationException>(
            () => ConsistencyParser.Parsear(json, new HashSet<int> { 1 }, 1));
    }
}
