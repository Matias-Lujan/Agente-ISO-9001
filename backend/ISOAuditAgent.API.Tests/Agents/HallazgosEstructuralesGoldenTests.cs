using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.ConsistencyVerification;
using ISOAuditAgent.API.Tests.Infra;

namespace ISOAuditAgent.API.Tests.Agents;

// Golden de HallazgosEstructurales.Generar (ConsistencyVerification). Función
// pura: compara secciones del documento contra el template. Congela el Caso 1
// (doc vacío → NC), el Caso 2 (sección ausente → OBS), el dedup por clave
// normalizada y la supresión de campos cuya sección padre está ausente.
public class HallazgosEstructuralesGoldenTests
{
    [Fact]
    public void Generar_CasosEstructurales_CoincideConGolden()
    {
        var artefactos = new[]
        {
            // Caso 1: documento completamente vacío (la sección sintética de
            // cabecera, con contenido, se excluye del diff). → NC/Procedimiento.
            Fx.Artefacto(1, "Documento vacío",
                doc: Fx.Doc("doc-vacio.docx"),
                seccionesDetectadas: new[]
                {
                    Fx.Sec("(Contenido inicial)", tieneContenido: true),
                    Fx.Sec("Introducción", tieneContenido: false),
                    Fx.Sec("Desarrollo", tieneContenido: false),
                },
                seccionesTemplate: new[]
                {
                    Fx.Sec("Introducción", tieneContenido: true),
                    Fx.Sec("Desarrollo", tieneContenido: true),
                }),

            // Caso 2: documento sustantivo con una sección del template ausente.
            // → OBS/Template para 'Conclusiones'.
            Fx.Artefacto(2, "Documento incompleto",
                doc: Fx.Doc("doc-incompleto.docx"),
                seccionesDetectadas: new[]
                {
                    Fx.Sec("Resumen", tieneContenido: true),
                },
                seccionesTemplate: new[]
                {
                    Fx.Sec("Resumen", tieneContenido: true),
                    Fx.Sec("Conclusiones", tieneContenido: true),
                }),

            // Supresión: el campo 'Riesgos › Descripción del riesgo' tiene su
            // sección padre 'Riesgos' ausente en el doc → NO se reporta el campo.
            Fx.Artefacto(3, "Campo con padre ausente",
                doc: Fx.Doc("doc-campos.docx"),
                seccionesDetectadas: new[]
                {
                    Fx.Sec("Resumen", tieneContenido: true),
                },
                seccionesTemplate: new[]
                {
                    Fx.Sec("Riesgos › Descripción del riesgo", tieneContenido: true),
                }),

            // Dedup: el template trae 'Descripción' y 'DESCRIPCIÓN' (misma clave
            // normalizada) → se reporta una sola ausencia, no dos.
            Fx.Artefacto(4, "Template con duplicados",
                doc: Fx.Doc("doc-dup.docx"),
                seccionesDetectadas: new[]
                {
                    Fx.Sec("Resumen", tieneContenido: true),
                },
                seccionesTemplate: new[]
                {
                    Fx.Sec("Descripción", tieneContenido: true),
                    Fx.Sec("DESCRIPCIÓN", tieneContenido: true),
                }),

            // Skip: sin template (SeccionesTemplate vacío) → no se evalúa.
            Fx.Artefacto(5, "Sin template",
                doc: Fx.Doc("doc-sin-template.docx"),
                seccionesDetectadas: new[]
                {
                    Fx.Sec("Algo", tieneContenido: false),
                }),
        };

        var hallazgos = HallazgosEstructurales.Generar(
            artefactos, "PR 11-13", "Desarrollo de Software");

        Snapshot.MatchJson(hallazgos, "HallazgosEstructurales.Generar");
    }
}
