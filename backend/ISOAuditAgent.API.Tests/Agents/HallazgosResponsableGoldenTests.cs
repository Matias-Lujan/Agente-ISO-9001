using ISOAuditAgent.API.Agents.ComplianceValidation;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Tests.Infra;

namespace ISOAuditAgent.API.Tests.Agents;

// Golden de HallazgosResponsable.Generar (ComplianceValidation). Función pura: si
// el Responsable de la PORTADA del tailoring está vacío, genera un OBS sobre el
// FR 29 (OrigenRegla.Responsable). Congela las ramas de la regla.
public class HallazgosResponsableGoldenTests
{
    [Fact]
    public void Generar_CasosResponsable_CoincideConGolden()
    {
        // A: responsable vacío + FR 29 exigible/aplica → OBS sobre FR 29.
        var conHallazgo = HallazgosResponsable.Generar(
            Fx.DocsResp(null,
                Fx.Artefacto(2, "Tailoring del Proyecto", codigo: "FR 29"),
                Fx.Artefacto(3, "ERS", codigo: "FR 30")));

        // B: responsable presente → sin hallazgo.
        var conResponsable = HallazgosResponsable.Generar(
            Fx.DocsResp("Juan Pérez",
                Fx.Artefacto(2, "Tailoring del Proyecto", codigo: "FR 29")));

        // C: responsable vacío pero FR 29 ausente de la lista → sin hallazgo.
        var sinFr29 = HallazgosResponsable.Generar(
            Fx.DocsResp(null,
                Fx.Artefacto(3, "ERS", codigo: "FR 30")));

        // D: responsable vacío + FR 29 NoAplica → sin hallazgo (no admite hallazgos).
        var fr29NoAplica = HallazgosResponsable.Generar(
            Fx.DocsResp(null,
                Fx.Artefacto(2, "Tailoring del Proyecto", codigo: "FR 29",
                    tailoring: EstadoAplicacionTailoring.NoAplica)));

        Snapshot.MatchJson(
            new { conHallazgo, conResponsable, sinFr29, fr29NoAplica },
            "HallazgosResponsable.Generar");
    }
}
