using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.ConsistencyVerification;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Tests;

// ============================================================================
//  Tests del DocxTemplateParser con documentos REALES de BDT como fixtures.
//
//  Fixtures (carpeta Fixtures/, copiados al output):
//   - FR30_template.docx / FR30_doc.docx  → ERS. Títulos de sección son
//     párrafos sin estilo seguidos de tablas (patrón "caption + tabla").
//     En el doc se eliminó la sección "Documentos relacionados".
//   - FR48_template.docx / FR48_doc.docx  → Sign-Off. Cada sección es una
//     tabla "tarjeta" (fila 0 = título). En el doc se eliminó "Entregables".
//
//  Los dos FR usan convenciones estructurales DISTINTAS: el parser debe
//  detectar las secciones en ambos sin depender de negrita ni tamaño de fuente
//  (garantía de adaptabilidad a procedimientos/documentos futuros).
// ============================================================================
public class DocxTemplateParserTests
{
    private static IReadOnlyList<SeccionDetectada> Parsear(string fixture)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture);
        var bytes = File.ReadAllBytes(path);
        return new DocxTemplateParser().Parsear(bytes);
    }

    private static bool TieneSeccion(IEnumerable<SeccionDetectada> secciones, string titulo)
    {
        var clave = TailoringColumnMapper.NormalizeHeaderKey(titulo);
        return secciones.Any(s =>
            TailoringColumnMapper.NormalizeHeaderKey(s.Titulo) == clave);
    }

    // ---- FR-30 (ERS): caption + tabla --------------------------------------

    [Fact]
    public void FR30_Template_DetectaSeccionesPorCaptionDeTabla()
    {
        var secciones = Parsear("FR30_template.docx");

        // Las secciones reales (no las columnas de sus tablas) se detectan.
        Assert.True(TieneSeccion(secciones, "Historial de Cambios"));
        Assert.True(TieneSeccion(secciones, "Documentos relacionados"));
        Assert.True(TieneSeccion(secciones, "Destinatarios"));
    }

    [Fact]
    public void FR30_Template_NoExponeColumnaComoSeccionDeNivelSuperior()
    {
        var secciones = Parsear("FR30_template.docx");

        // "Documento" (columna de la tabla "Documentos relacionados") NO debe
        // aparecer como sección suelta: debe estar calificada por su padre.
        Assert.False(TieneSeccion(secciones, "Documento"));
        Assert.True(TieneSeccion(secciones, "Documentos relacionados › Documento"));
    }

    [Fact]
    public void FR30_Doc_NoTieneLaSeccionEliminada()
    {
        var secciones = Parsear("FR30_doc.docx");

        // En el doc real se eliminó la sección "Documentos relacionados".
        Assert.False(TieneSeccion(secciones, "Documentos relacionados"));
        // Pero las otras secciones siguen presentes.
        Assert.True(TieneSeccion(secciones, "Historial de Cambios"));
    }

    // ---- FR-48 (Sign-Off): tablas tarjeta ----------------------------------

    [Fact]
    public void FR48_Template_DetectaTitulosDeTarjeta()
    {
        var secciones = Parsear("FR48_template.docx");

        Assert.True(TieneSeccion(secciones, "Resumen"));
        Assert.True(TieneSeccion(secciones, "Entregables"));
        Assert.True(TieneSeccion(secciones, "Aceptación de la finalización del proyecto"));
    }

    [Fact]
    public void FR48_Doc_NoTieneEntregables()
    {
        var secciones = Parsear("FR48_doc.docx");

        Assert.False(TieneSeccion(secciones, "Entregables"));
        // Las secciones que quedaron siguen detectándose.
        Assert.True(TieneSeccion(secciones, "Resumen"));
    }

    // ---- Integración con HallazgosEstructurales -----------------------------

    [Fact]
    public void HallazgosEstructurales_FR48_GeneraOBSporEntregablesAusente()
    {
        var artefacto = ArtefactoConSecciones(
            nombre: "Sign-Off",
            codigo: "FR 48",
            seccionesDoc: Parsear("FR48_doc.docx"),
            seccionesTemplate: Parsear("FR48_template.docx"));

        var hallazgos = HallazgosEstructurales.Generar(
            new[] { artefacto }, "PR 11-13", "Desarrollo de Software");

        Assert.Contains(hallazgos, h =>
            h.Descripcion.Contains("Entregables", StringComparison.OrdinalIgnoreCase)
            && h.OrigenRegla == OrigenRegla.Template);
    }

    [Fact]
    public void HallazgosEstructurales_FR30_NoGeneraRuidoDeCamposCuandoFaltaLaSeccion()
    {
        var artefacto = ArtefactoConSecciones(
            nombre: "ERS",
            codigo: "FR 30",
            seccionesDoc: Parsear("FR30_doc.docx"),
            seccionesTemplate: Parsear("FR30_template.docx"));

        var hallazgos = HallazgosEstructurales.Generar(
            new[] { artefacto }, "PR 11-13", "Desarrollo de Software");

        // Reporta la sección padre ausente...
        Assert.Contains(hallazgos, h =>
            h.Descripcion.Contains("Documentos relacionados", StringComparison.OrdinalIgnoreCase));

        // ...pero NO reporta sus campos por separado (supresión de ruido).
        Assert.DoesNotContain(hallazgos, h =>
            h.Descripcion.Contains("Documentos relacionados › Documento", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HallazgosEstructurales_NoSuprimeSeccionConBarraEnElNombre()
    {
        // Regresión: una columna real puede contener "/" en su nombre (ej. el
        // FR-31 tiene "Descripcion Tarea / Riesgo"). Eso NO es un campo calificado
        // ("padre › campo") y no debe suprimirse aunque "Descripcion Tarea" no
        // exista como sección en el documento.
        var artefacto = ArtefactoConSecciones(
            nombre: "Matriz de Riesgo",
            codigo: "FR 31",
            seccionesDoc: new[]
            {
                new SeccionDetectada("Identificador", true),
                new SeccionDetectada("Probabilidad", true),
            },
            seccionesTemplate: new[]
            {
                new SeccionDetectada("Identificador", true),
                new SeccionDetectada("Probabilidad", true),
                new SeccionDetectada("Descripcion Tarea / Riesgo", false),
            });

        var hallazgos = HallazgosEstructurales.Generar(
            new[] { artefacto }, "PR 11-13", "Desarrollo de Software");

        Assert.Contains(hallazgos, h =>
            h.Descripcion.Contains("Descripcion Tarea / Riesgo", StringComparison.OrdinalIgnoreCase));
    }

    private static ArtefactoExtraido ArtefactoConSecciones(
        string nombre,
        string codigo,
        IReadOnlyList<SeccionDetectada> seccionesDoc,
        IReadOnlyList<SeccionDetectada> seccionesTemplate)
    {
        return new ArtefactoExtraido(
            ArtefactoEsperadoId: 1,
            CodigoArtefacto: codigo,
            NombreArtefacto: nombre,
            EtapaArtefactoId: 1,
            Exigibilidad: ExigibilidadArtefacto.Exigible,
            Obligatoriedad: ObligatoriedadArtefacto.Mandatorio,
            EstadoAplicacionTailoring: EstadoAplicacionTailoring.Aplica,
            JustificacionNoAplica: null,
            EstadoDisponibilidad: EstadoDisponibilidad.Encontrado,
            UrlReferencia: null,
            NombreTemplateArchivo: $"{codigo}.docx",
            DocumentoEncontrado: new DocumentoEncontrado(
                $"{nombre}.docx", FuenteDocumento.Drive, new string('a', 64)),
            SeccionesDetectadas: seccionesDoc,
            SeccionesTemplate: seccionesTemplate,
            Descripcion: null);
    }
}
