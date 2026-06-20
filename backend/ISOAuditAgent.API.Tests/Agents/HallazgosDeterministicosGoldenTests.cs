using ISOAuditAgent.API.Agents.ComplianceValidation;
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Tests.Infra;

namespace ISOAuditAgent.API.Tests.Agents;

// Golden de HallazgosDeterministicos.Generar (ComplianceValidation). Función
// pura: DocumentosExtraidos fijo → lista idéntica de hallazgos. Congela las
// 4 reglas determinísticas (incl. textos de justificación verbatim) y el
// filtrado por exigibilidad. Cualquier cambio de comportamiento rompe el snapshot.
public class HallazgosDeterministicosGoldenTests
{
    [Fact]
    public void Generar_TodasLasReglas_CoincideConGolden()
    {
        var input = Fx.Docs(
            // Regla 1: Aplica + Faltante, sin URL de referencia.
            Fx.Artefacto(1, "ERS sin URL",
                tailoring: EstadoAplicacionTailoring.Aplica,
                disponibilidad: EstadoDisponibilidad.Faltante),

            // Regla 1: Aplica + Faltante, con URL de referencia.
            Fx.Artefacto(2, "ERS con URL",
                tailoring: EstadoAplicacionTailoring.Aplica,
                disponibilidad: EstadoDisponibilidad.Faltante,
                urlReferencia: "https://drive.google.com/file/d/abc"),

            // Regla 2: NoAplica sin justificación → hallazgo.
            Fx.Artefacto(3, "Plan de pruebas",
                tailoring: EstadoAplicacionTailoring.NoAplica,
                justificacionNoAplica: null,
                disponibilidad: EstadoDisponibilidad.NoBuscado),

            // Control regla 2: NoAplica CON justificación → NO hallazgo.
            Fx.Artefacto(4, "Acta de cierre",
                tailoring: EstadoAplicacionTailoring.NoAplica,
                justificacionNoAplica: "El proyecto no requiere acta de cierre formal.",
                disponibilidad: EstadoDisponibilidad.NoBuscado),

            // Regla 4: Aplica + ExportFallido.
            Fx.Artefacto(5, "Diagrama nativo Google",
                tailoring: EstadoAplicacionTailoring.Aplica,
                disponibilidad: EstadoDisponibilidad.ExportFallido),

            // Regla 3: SinDeclararEnTailoring, con código y mandatorio.
            Fx.Artefacto(6, "Sign-Off", codigo: "FR-48",
                obligatoriedad: ObligatoriedadArtefacto.Mandatorio,
                tailoring: EstadoAplicacionTailoring.SinDeclararEnTailoring,
                disponibilidad: EstadoDisponibilidad.NoBuscado),

            // Regla 3: SinDeclararEnTailoring, sin código y evaluar/justificar.
            Fx.Artefacto(7, "Minuta de reunión",
                obligatoriedad: ObligatoriedadArtefacto.EvaluarYJustificar,
                tailoring: EstadoAplicacionTailoring.SinDeclararEnTailoring,
                disponibilidad: EstadoDisponibilidad.NoBuscado),

            // Filtrado: PendienteEtapaFutura → ninguna regla aplica.
            Fx.Artefacto(8, "Artefacto etapa futura",
                exigibilidad: ExigibilidadArtefacto.PendienteEtapaFutura,
                tailoring: EstadoAplicacionTailoring.Aplica,
                disponibilidad: EstadoDisponibilidad.Faltante));

        var hallazgos = HallazgosDeterministicos.Generar(input);

        Snapshot.MatchJson(hallazgos, "HallazgosDeterministicos.Generar");
    }
}
