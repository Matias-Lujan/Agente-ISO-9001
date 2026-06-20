using ISOAuditAgent.API.Agents.ComplianceValidation;
using ISOAuditAgent.API.Agents.ConsistencyVerification;
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.FindingsClassification;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Tests.Infra;

namespace ISOAuditAgent.API.Tests.Agents;

// Golden de los tres *PromptBuilder.Construir. Son funciones puras (input fijo →
// string byte-idéntico). Congelar el prompt completo es la red de seguridad más
// fuerte para el refactor de los agentes: cualquier cambio en cómo se le habla
// al LLM rompe el snapshot. Se cubren también los filtros EsCandidatoLlm.
public class PromptBuildersGoldenTests
{
    // ======================= ComplianceValidation =======================

    [Fact]
    public void CompliancePrompt_ConCandidatos_CoincideConGolden()
    {
        var input = Fx.Docs(
            Fx.Artefacto(1, "ERS", codigo: "FR-30",
                tailoring: EstadoAplicacionTailoring.Aplica,
                disponibilidad: EstadoDisponibilidad.Encontrado,
                urlReferencia: "https://drive.google.com/file/d/AAA/view",
                doc: Fx.Doc("ERS_proyecto.docx")),
            Fx.Artefacto(2, "Plan de proyecto", codigo: "FR-12",
                tailoring: EstadoAplicacionTailoring.Aplica,
                disponibilidad: EstadoDisponibilidad.Encontrado,
                urlReferencia: "https://drive.google.com/file/d/BBB/view",
                doc: Fx.Doc("Plan.xlsx")),
            // No candidato (folder URL) → no debe aparecer en el prompt.
            Fx.Artefacto(3, "Carpeta evidencias",
                tailoring: EstadoAplicacionTailoring.Aplica,
                disponibilidad: EstadoDisponibilidad.Encontrado,
                urlReferencia: "https://drive.google.com/drive/folders/CCC",
                doc: Fx.Doc("algo.docx")));

        Snapshot.Match(CompliancePromptBuilder.Construir(input), "CompliancePrompt.ConCandidatos");
    }

    [Fact]
    public void CompliancePrompt_SinCandidatos_DevuelvePromptVacio()
    {
        var input = Fx.Docs(
            Fx.Artefacto(1, "ERS",
                tailoring: EstadoAplicacionTailoring.Aplica,
                disponibilidad: EstadoDisponibilidad.Faltante));

        Assert.Equal(
            "No hay artefactos para analizar. Respondé exactamente: {\"hallazgos\": []}",
            CompliancePromptBuilder.Construir(input));
    }

    [Fact]
    public void Compliance_EsCandidatoLlm_FiltraCorrectamente()
    {
        // Candidato válido.
        Assert.True(CompliancePromptBuilder.EsCandidatoLlm(Candidato()));

        // PendienteEtapaFutura → fuera.
        Assert.False(CompliancePromptBuilder.EsCandidatoLlm(
            Candidato() with { Exigibilidad = ExigibilidadArtefacto.PendienteEtapaFutura }));

        // NoAplica → fuera.
        Assert.False(CompliancePromptBuilder.EsCandidatoLlm(
            Candidato() with { EstadoAplicacionTailoring = EstadoAplicacionTailoring.NoAplica }));

        // No encontrado → fuera.
        Assert.False(CompliancePromptBuilder.EsCandidatoLlm(
            Candidato() with { EstadoDisponibilidad = EstadoDisponibilidad.Faltante }));

        // Fuente Trello (no Drive) → fuera.
        Assert.False(CompliancePromptBuilder.EsCandidatoLlm(
            Candidato() with { DocumentoEncontrado = Fx.Doc("card", FuenteDocumento.Trello) }));

        // URL de carpeta → fuera.
        Assert.False(CompliancePromptBuilder.EsCandidatoLlm(
            Candidato() with { UrlReferencia = "https://drive.google.com/drive/folders/X" }));

        // URL vacía → fuera.
        Assert.False(CompliancePromptBuilder.EsCandidatoLlm(
            Candidato() with { UrlReferencia = null }));
    }

    private static ArtefactoExtraido Candidato() =>
        Fx.Artefacto(1, "ERS",
            tailoring: EstadoAplicacionTailoring.Aplica,
            disponibilidad: EstadoDisponibilidad.Encontrado,
            urlReferencia: "https://drive.google.com/file/d/AAA/view",
            doc: Fx.Doc("ERS.docx"));

    // ======================= ConsistencyVerification =======================

    [Fact]
    public void ConsistencyPrompt_ConCandidatos_CoincideConGolden()
    {
        var input = Fx.Docs(
            Fx.Artefacto(1, "Sign-Off", codigo: "FR-48",
                disponibilidad: EstadoDisponibilidad.Encontrado,
                doc: Fx.Doc("SignOff.docx"),
                descripcion: "Describe los entregables del proyecto al cliente.",
                seccionesDetectadas: new[]
                {
                    Fx.Sec("Resumen", tieneContenido: true),
                    Fx.Sec("Entregables", tieneContenido: false),
                }),
            // No candidato (sin secciones vacías) → no aparece.
            Fx.Artefacto(2, "Acta",
                disponibilidad: EstadoDisponibilidad.Encontrado,
                doc: Fx.Doc("Acta.docx"),
                seccionesDetectadas: new[] { Fx.Sec("Cuerpo", tieneContenido: true) }));

        Snapshot.Match(ConsistencyPromptBuilder.Construir(input), "ConsistencyPrompt.ConCandidatos");
    }

    [Fact]
    public void ConsistencyPrompt_SinCandidatos_DevuelvePromptVacio()
    {
        var input = Fx.Docs(
            Fx.Artefacto(1, "Acta",
                disponibilidad: EstadoDisponibilidad.Encontrado,
                doc: Fx.Doc("Acta.docx"),
                seccionesDetectadas: new[] { Fx.Sec("Cuerpo", tieneContenido: true) }));

        Assert.Equal(
            "No hay secciones vacías para analizar. Respondé exactamente: {\"hallazgos\": []}",
            ConsistencyPromptBuilder.Construir(input));
    }

    [Fact]
    public void Consistency_EsCandidatoLlm_FiltraCorrectamente()
    {
        var conSeccionVacia = new[]
        {
            Fx.Sec("Resumen", tieneContenido: true),
            Fx.Sec("Entregables", tieneContenido: false),
        };

        var candidato = Fx.Artefacto(1, "Sign-Off",
            disponibilidad: EstadoDisponibilidad.Encontrado,
            doc: Fx.Doc("SignOff.docx"),
            seccionesDetectadas: conSeccionVacia);

        Assert.True(ConsistencyPromptBuilder.EsCandidatoLlm(candidato));

        // PendienteEtapaFutura → fuera.
        Assert.False(ConsistencyPromptBuilder.EsCandidatoLlm(
            candidato with { Exigibilidad = ExigibilidadArtefacto.PendienteEtapaFutura }));

        // No encontrado → fuera.
        Assert.False(ConsistencyPromptBuilder.EsCandidatoLlm(
            candidato with { EstadoDisponibilidad = EstadoDisponibilidad.Faltante }));

        // Todas las secciones con contenido → fuera.
        Assert.False(ConsistencyPromptBuilder.EsCandidatoLlm(
            candidato with { SeccionesDetectadas = new[] { Fx.Sec("Resumen", true) } }));
    }

    // ======================= FindingsClassification =======================

    [Fact]
    public void FindingsPrompt_VariosLotes_CoincideConGolden()
    {
        // Contexto: el artefacto 2 trae propósito y archivo → ejercita el bloque
        // extra que se agrega solo a los hallazgos de OrigenRegla.Template.
        var contexto = Fx.Docs(
            Fx.Artefacto(1, "ERS", codigo: "FR-30"),
            Fx.Artefacto(2, "Sign-Off", codigo: "FR-48",
                doc: Fx.Doc("SignOff.docx"),
                descripcion: "Describe los entregables del proyecto al cliente."));

        var lotes = new[]
        {
            Fx.Lote(AgenteOrigen.ComplianceValidation,
                Fx.Preliminar(1, OrigenRegla.Procedimiento,
                    "Artefacto faltante", "El tailoring lo declara Aplica y no se encontró."),
                Fx.Preliminar(2, OrigenRegla.Tailoring,
                    "Incoherencia URL", "La URL del tailoring no coincide con el archivo.")),
            Fx.Lote(AgenteOrigen.ConsistencyVerification,
                Fx.Preliminar(2, OrigenRegla.Template,
                    "Sección 'Entregables' ausente", "Justificación genérica del C#.")),
        };

        Snapshot.Match(FindingsPromptBuilder.Construir(lotes, contexto), "FindingsPrompt.VariosLotes");
    }
}
