using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Agent.Validation;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Agent;

public sealed class InvariantsValidatorTests
{
    [Fact]
    public void Falla_si_ids_raiz_invalidos()
    {
        var dto = new DocumentosExtraidos(0, 1, 1, BuildValidArtefactos(1));
        var ex = Assert.Throws<InvariantViolationException>(() => InvariantsValidator.Validate(dto));
        Assert.Contains("Identificadores", ex.Message);
    }

    [Fact]
    public void Falla_si_Encontrado_sin_DocumentoEncontrado()
    {
        var a = new ArtefactoExtraido(
            1, "FR 1", "N", 1,
            ExigibilidadArtefacto.Exigible, ObligatoriedadArtefacto.Mandatorio,
            EstadoTailoring.Aplica, null,
            EstadoDisponibilidad.Encontrado,
            null, null, null, Array.Empty<SeccionDetectada>());
        var dto = new DocumentosExtraidos(1, 1, 1, new[] { a });
        Assert.Throws<InvariantViolationException>(() => InvariantsValidator.Validate(dto));
    }

    [Fact]
    public void Falla_si_Faltante_con_DocumentoEncontrado()
    {
        var doc = new DocumentoEncontrado("a.docx", FuenteDocumento.Drive,
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var a = new ArtefactoExtraido(
            1, "FR 1", "N", 1,
            ExigibilidadArtefacto.Exigible, ObligatoriedadArtefacto.Mandatorio,
            EstadoTailoring.Aplica, null,
            EstadoDisponibilidad.Faltante,
            null, null, doc, Array.Empty<SeccionDetectada>());
        var dto = new DocumentosExtraidos(1, 1, 1, new[] { a });
        Assert.Throws<InvariantViolationException>(() => InvariantsValidator.Validate(dto));
    }

    [Fact]
    public void Falla_si_PendienteEtapaFutura_no_es_NoBuscado()
    {
        var a = MinimalArtefacto(1, ExigibilidadArtefacto.PendienteEtapaFutura,
            EstadoDisponibilidad.Faltante);
        var dto = new DocumentosExtraidos(1, 1, 1, new[] { a });
        Assert.Throws<InvariantViolationException>(() => InvariantsValidator.Validate(dto));
    }

    [Fact]
    public void Falla_si_hay_ArtefactoEsperadoId_duplicado()
    {
        var a = BuildValidArtefactos(1)[0];
        var dto = new DocumentosExtraidos(1, 1, 1, new[] { a, a });
        Assert.Throws<InvariantViolationException>(() => InvariantsValidator.Validate(dto));
    }

    [Fact]
    public void Falla_si_SeccionesDetectadas_es_null()
    {
        var a = new ArtefactoExtraido(
            1, "FR 1", "N", 1,
            ExigibilidadArtefacto.Exigible, ObligatoriedadArtefacto.Mandatorio,
            EstadoTailoring.SinDeclararEnTailoring, null,
            EstadoDisponibilidad.NoBuscado,
            null, null, null,
            null!);
        var dto = new DocumentosExtraidos(1, 1, 1, new[] { a });
        Assert.Throws<InvariantViolationException>(() => InvariantsValidator.Validate(dto));
    }

    [Fact]
    public void Falla_si_Hash_invalido()
    {
        var doc = new DocumentoEncontrado("a.docx", FuenteDocumento.Drive, "NOT_A_HASH");
        var a = new ArtefactoExtraido(
            1, "FR 1", "N", 1,
            ExigibilidadArtefacto.Exigible, ObligatoriedadArtefacto.Mandatorio,
            EstadoTailoring.Aplica, null,
            EstadoDisponibilidad.Encontrado,
            null, null, doc, Array.Empty<SeccionDetectada>());
        var dto = new DocumentosExtraidos(1, 1, 1, new[] { a });
        Assert.Throws<InvariantViolationException>(() => InvariantsValidator.Validate(dto));
    }

    [Fact]
    public void Falla_si_Justificacion_con_estado_distinto_de_NoAplica()
    {
        var a = new ArtefactoExtraido(
            1, "FR 1", "N", 1,
            ExigibilidadArtefacto.Exigible, ObligatoriedadArtefacto.Mandatorio,
            EstadoTailoring.Aplica, "no debería haber texto",
            EstadoDisponibilidad.NoBuscado,
            null, null, null, Array.Empty<SeccionDetectada>());
        var dto = new DocumentosExtraidos(1, 1, 1, new[] { a });
        Assert.Throws<InvariantViolationException>(() => InvariantsValidator.Validate(dto));
    }

    [Fact]
    public void Pasa_DTO_minimo_valido_NoBuscado()
    {
        var dto = new DocumentosExtraidos(1, 1, 1, BuildValidArtefactos(1));
        InvariantsValidator.Validate(dto);
    }

    private static IReadOnlyList<ArtefactoExtraido> BuildValidArtefactos(int id) =>
        new[]
        {
            new ArtefactoExtraido(
                id, "FR 1", "Nombre", 1,
                ExigibilidadArtefacto.Exigible, ObligatoriedadArtefacto.Mandatorio,
                EstadoTailoring.SinDeclararEnTailoring, null,
                EstadoDisponibilidad.NoBuscado,
                null, null, null, Array.Empty<SeccionDetectada>())
        };

    private static ArtefactoExtraido MinimalArtefacto(
        int id,
        ExigibilidadArtefacto ex,
        EstadoDisponibilidad disp) =>
        new(
            id, "C", "N", 1,
            ex, ObligatoriedadArtefacto.Mandatorio,
            EstadoTailoring.SinDeclararEnTailoring, null,
            disp,
            null, null, null, Array.Empty<SeccionDetectada>());
}
