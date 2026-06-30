using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.ConsistencyVerification;
using ISOAuditAgent.API.Tests.Infra;

namespace ISOAuditAgent.API.Tests.Agents;

// Golden de HallazgosVigencia.Generar (ConsistencyVerification). Función pura:
// compara la VigenciaDetectada del documento contra la VigenciaEsperada de la
// referencia de Calidad. Congela las ramas de la regla acordada (todos los
// hallazgos llevan OrigenRegla.Vigencia → OBS aguas abajo).
public class HallazgosVigenciaGoldenTests
{
    [Fact]
    public void Generar_CasosVigencia_CoincideConGolden()
    {
        var artefactos = new[]
        {
            // 1. Vigencia del documento distinta de la vigente → OBS (formulario desactualizado).
            Fx.Artefacto(1, "ERS", codigo: "FR 30",
                doc: Fx.Doc("FR 30-02 ERS.docx"),
                documentoParseable: true,
                vigenciaDetectada: new DateOnly(2020, 8, 13),
                vigenciaEsperada: new DateOnly(2025, 1, 15)),

            // 2. Vigencias iguales → Conforme, sin hallazgo.
            Fx.Artefacto(2, "Sign-Off", codigo: "FR 48",
                doc: Fx.Doc("FR 48-01 Sign Off.docx"),
                documentoParseable: true,
                vigenciaDetectada: new DateOnly(2021, 7, 1),
                vigenciaEsperada: new DateOnly(2021, 7, 1)),

            // 3. Parseable pero sin vigencia detectada en el documento → OBS.
            Fx.Artefacto(3, "Matriz de Riesgo", codigo: "FR 31",
                doc: Fx.Doc("FR 31-03 Matriz.xlsx"),
                documentoParseable: true,
                vigenciaDetectada: null,
                vigenciaEsperada: new DateOnly(2022, 3, 1)),

            // 4. Sin referencia de Calidad (esperada null) → sin hallazgo (solo log).
            Fx.Artefacto(4, "Control de Costos", codigo: "FR 71",
                doc: Fx.Doc("FR 71-01 Costos.xlsx"),
                documentoParseable: true,
                vigenciaDetectada: new DateOnly(2020, 1, 1),
                vigenciaEsperada: null),

            // 5. Formato no parseable (DocumentoParseable=false) → sin hallazgo (solo log).
            Fx.Artefacto(5, "Cronograma", codigo: null,
                doc: Fx.Doc("cronograma.mpp"),
                documentoParseable: false,
                vigenciaDetectada: null,
                vigenciaEsperada: new DateOnly(2023, 5, 5)),

            // 6. No exigible (etapa futura) → sin hallazgo aunque la referencia exista.
            Fx.Artefacto(6, "Encuesta de Sign-Off", codigo: "FR 85",
                exigibilidad: ExigibilidadArtefacto.PendienteEtapaFutura,
                disponibilidad: EstadoDisponibilidad.NoBuscado,
                vigenciaEsperada: new DateOnly(2024, 4, 4)),

            // 7. Exigible pero no Encontrado (Faltante) → sin hallazgo de vigencia.
            Fx.Artefacto(7, "Liberación de Software", codigo: "FR 25",
                disponibilidad: EstadoDisponibilidad.Faltante,
                vigenciaEsperada: new DateOnly(2024, 6, 6)),
        };

        var hallazgos = HallazgosVigencia.Generar(artefactos);

        Snapshot.MatchJson(hallazgos, "HallazgosVigencia.Generar");
    }
}
