using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.FindingsClassification;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Tests.Infra;

namespace ISOAuditAgent.API.Tests.Agents;

// Caracterización de ClasificacionResponseParser.Parsear. Mezcla asserts
// explícitos (reglas de negocio críticas + invariantes que fuerzan fallo) con
// un snapshot del mapeo completo. Es la pieza más sensible del refactor: acá
// vive la regla de oro y la correspondencia 1-a-1 por índice.
public class ClasificacionResponseParserTests
{
    // ----- Regla de oro: degradación NC → OM SOLO para Tailoring -----

    [Theory]
    [InlineData(OrigenRegla.Tailoring, "NC", TipoHallazgo.OM)]   // degradado
    [InlineData(OrigenRegla.Tailoring, "OBS", TipoHallazgo.OBS)] // no se toca
    [InlineData(OrigenRegla.Tailoring, "OM", TipoHallazgo.OM)]
    [InlineData(OrigenRegla.Procedimiento, "NC", TipoHallazgo.NC)] // sobrevive
    [InlineData(OrigenRegla.Template, "NC", TipoHallazgo.NC)]      // sobrevive
    [InlineData(OrigenRegla.Procedimiento, "OBS", TipoHallazgo.OBS)]
    [InlineData(OrigenRegla.Template, "nc", TipoHallazgo.NC)]      // case-insensitive
    [InlineData(OrigenRegla.Procedimiento, "loquesea", TipoHallazgo.OBS)] // default conservador
    public void Parsear_ResuelveTipo_AplicaReglaDeOro(
        OrigenRegla origen, string tipoLlm, TipoHallazgo esperado)
    {
        var resultado = ParsearUno(origen, tipoLlm);

        Assert.Equal(esperado, resultado.Tipo);
    }

    // ----- Justificación: Procedimiento mantiene la original; el resto usa la del LLM -----

    [Fact]
    public void Parsear_OrigenProcedimiento_IgnoraJustificacionDelLlm()
    {
        var resultado = ParsearUno(
            OrigenRegla.Procedimiento, "NC",
            justifLlm: "justificación inventada por el LLM",
            justifOriginal: "justificación determinística original");

        Assert.Equal("justificación determinística original", resultado.Justificacion);
    }

    [Fact]
    public void Parsear_OrigenTemplate_UsaJustificacionDelLlm()
    {
        var resultado = ParsearUno(
            OrigenRegla.Template, "OBS",
            justifLlm: "la sección Entregables es esencial al Sign-Off",
            justifOriginal: "justificación genérica del C#");

        Assert.Equal("la sección Entregables es esencial al Sign-Off", resultado.Justificacion);
    }

    [Fact]
    public void Parsear_OrigenTemplate_JustificacionLlmVacia_CaeEnLaOriginal()
    {
        var resultado = ParsearUno(
            OrigenRegla.Template, "OBS",
            justifLlm: "   ",
            justifOriginal: "justificación genérica del C#");

        Assert.Equal("justificación genérica del C#", resultado.Justificacion);
    }

    // ----- Invariantes: cualquier desalineación con el LLM debe FALLAR -----

    [Fact]
    public void Parsear_JsonInvalido_Lanza()
    {
        var planos = Planos((OrigenRegla.Procedimiento, AgenteOrigen.ComplianceValidation));
        Assert.Throws<InvalidOperationException>(
            () => ClasificacionResponseParser.Parsear("esto no es json", planos, 1));
    }

    [Fact]
    public void Parsear_RaizNoEsArray_Lanza()
    {
        var planos = Planos((OrigenRegla.Procedimiento, AgenteOrigen.ComplianceValidation));
        Assert.Throws<InvalidOperationException>(
            () => ClasificacionResponseParser.Parsear("{\"indice\":0}", planos, 1));
    }

    [Fact]
    public void Parsear_CantidadDistinta_Lanza()
    {
        var planos = Planos(
            (OrigenRegla.Procedimiento, AgenteOrigen.ComplianceValidation),
            (OrigenRegla.Template, AgenteOrigen.ConsistencyVerification));
        var json = "[{ \"indice\": 0, \"tipo\": \"NC\" }]"; // 1 item, esperaba 2
        Assert.Throws<InvalidOperationException>(
            () => ClasificacionResponseParser.Parsear(json, planos, 1));
    }

    [Fact]
    public void Parsear_IndiceDuplicado_Lanza()
    {
        var planos = Planos(
            (OrigenRegla.Procedimiento, AgenteOrigen.ComplianceValidation),
            (OrigenRegla.Template, AgenteOrigen.ConsistencyVerification));
        var json = "[{ \"indice\": 0, \"tipo\": \"NC\" }, { \"indice\": 0, \"tipo\": \"OBS\" }]";
        Assert.Throws<InvalidOperationException>(
            () => ClasificacionResponseParser.Parsear(json, planos, 1));
    }

    [Fact]
    public void Parsear_IndiceFueraDeRango_Lanza()
    {
        var planos = Planos((OrigenRegla.Procedimiento, AgenteOrigen.ComplianceValidation));
        var json = "[{ \"indice\": 5, \"tipo\": \"NC\" }]";
        Assert.Throws<InvalidOperationException>(
            () => ClasificacionResponseParser.Parsear(json, planos, 1));
    }

    [Fact]
    public void Parsear_ItemSinIndice_Lanza()
    {
        var planos = Planos((OrigenRegla.Procedimiento, AgenteOrigen.ComplianceValidation));
        var json = "[{ \"tipo\": \"NC\" }]";
        Assert.Throws<InvalidOperationException>(
            () => ClasificacionResponseParser.Parsear(json, planos, 1));
    }

    // ----- Mapeo completo: snapshot del DTO de salida -----

    [Fact]
    public void Parsear_MapeoCompleto_CoincideConGolden()
    {
        var planos = new (HallazgoPreliminar, AgenteOrigen)[]
        {
            (Fx.Preliminar(10, OrigenRegla.Procedimiento, "Artefacto faltante", "just proc"),
                AgenteOrigen.ComplianceValidation),
            (Fx.Preliminar(11, OrigenRegla.Template, "Sección ausente", "just template generica"),
                AgenteOrigen.ConsistencyVerification),
            (Fx.Preliminar(12, OrigenRegla.Tailoring, "Incoherencia URL", "just tailoring"),
                AgenteOrigen.ComplianceValidation),
        };

        // El LLM responde desordenado y, por las dudas, clasifica el Tailoring
        // como NC (debe degradarse) y reescribe la justificación del Template.
        var json = """
            ```json
            [
              { "indice": 2, "tipo": "NC", "justificacion": "intento de NC en tailoring" },
              { "indice": 0, "tipo": "NC", "justificacion": "el LLM no debe pisar esto" },
              { "indice": 1, "tipo": "OBS", "justificacion": "sección Entregables esencial" }
            ]
            ```
            """;

        var resultado = ClasificacionResponseParser.Parsear(json, planos, auditoriaId: 99);

        Snapshot.MatchJson(resultado, "ClasificacionResponseParser.Parsear");
    }

    // ----- Helpers -----

    private static HallazgoClasificado ParsearUno(
        OrigenRegla origen, string tipoLlm,
        string justifLlm = "justif del llm", string justifOriginal = "justif original")
    {
        var planos = new (HallazgoPreliminar, AgenteOrigen)[]
        {
            (Fx.Preliminar(42, origen, "desc", justifOriginal), AgenteOrigen.ComplianceValidation)
        };
        var json = $$"""[{ "indice": 0, "tipo": "{{tipoLlm}}", "justificacion": "{{justifLlm}}" }]""";
        return ClasificacionResponseParser.Parsear(json, planos, 7).Hallazgos[0];
    }

    private static (HallazgoPreliminar, AgenteOrigen)[] Planos(
        params (OrigenRegla Origen, AgenteOrigen Agente)[] specs) =>
        specs.Select(s =>
            (Fx.Preliminar(1, s.Origen), s.Agente)).ToArray();
}
