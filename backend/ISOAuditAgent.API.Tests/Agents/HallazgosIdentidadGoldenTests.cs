using ISOAuditAgent.API.Agents.ComplianceValidation;
using ISOAuditAgent.API.Tests.Infra;

namespace ISOAuditAgent.API.Tests.Agents;

// Golden de HallazgosIdentidad.Generar (ComplianceValidation). Función pura:
// código-match (código FR interno vs esperado) + proyecto-match (proyecto interno
// vs auditado), ambos → NC (OrigenRegla.Identidad). Congela las ramas.
public class HallazgosIdentidadGoldenTests
{
    [Fact]
    public void Generar_CasosIdentidad_CoincideConGolden()
    {
        // A: código interno distinto del esperado → NC (proyecto OK).
        var codigoMismatch = HallazgosIdentidad.Generar(
            Fx.DocsProyecto("App Productores",
                Fx.Artefacto(3, "ERS", codigo: "FR 30",
                    codigoDetectado: "FR 31-03",
                    proyectoDetectado: "App Productores")));

        // B: código interno coincide (ignora versión) → nada.
        var codigoOk = HallazgosIdentidad.Generar(
            Fx.DocsProyecto("App Productores",
                Fx.Artefacto(3, "ERS", codigo: "FR 30",
                    codigoDetectado: "FR 30-02",
                    proyectoDetectado: "App Productores")));

        // C: documento de otro proyecto → NC (código OK).
        var proyectoMismatch = HallazgosIdentidad.Generar(
            Fx.DocsProyecto("App Productores",
                Fx.Artefacto(16, "Sign-Off", codigo: "FR 48",
                    codigoDetectado: "FR 48-01",
                    proyectoDetectado: "App PAS")));

        // D: proyecto interno contiene al auditado ("30052 - App Productores") → nada.
        var proyectoOk = HallazgosIdentidad.Generar(
            Fx.DocsProyecto("App Productores",
                Fx.Artefacto(4, "Matriz", codigo: "FR 31",
                    codigoDetectado: "FR 31-03",
                    proyectoDetectado: "30052 - App Productores")));

        // E: sin código ni proyecto detectado → no comparable → nada.
        var noComparable = HallazgosIdentidad.Generar(
            Fx.DocsProyecto("App Productores",
                Fx.Artefacto(5, "Cronograma", codigo: null)));

        Snapshot.MatchJson(
            new { codigoMismatch, codigoOk, proyectoMismatch, proyectoOk, noComparable },
            "HallazgosIdentidad.Generar");
    }
}
