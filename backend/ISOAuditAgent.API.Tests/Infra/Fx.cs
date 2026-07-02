using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Tests.Infra;

// ============================================================================
//  Fx — fábricas de fixtures para los contratos del workflow.
// ----------------------------------------------------------------------------
//  Builders con defaults sensatos para construir DocumentosExtraidos /
//  ArtefactoExtraido / hallazgos sin repetir los 16 parámetros posicionales en
//  cada test. Solo se sobreescribe lo que el caso necesita ejercitar.
// ============================================================================
internal static class Fx
{
    public static DocumentosExtraidos Docs(params ArtefactoExtraido[] artefactos) =>
        DocsResp("Responsable Demo", artefactos);

    // Variante que controla el Responsable de la portada del tailoring (para los
    // tests de HallazgosResponsable). Docs() usa un responsable no vacío por default.
    public static DocumentosExtraidos DocsResp(
        string? responsableTailoring, params ArtefactoExtraido[] artefactos) => new(
            AuditoriaId: 100,
            ProyectoId: 200,
            EtapaId: 300,
            ProcedimientoCodigo: "PR 11-13",
            ProcedimientoNombre: "Desarrollo de Software",
            Artefactos: artefactos,
            ResponsableTailoring: responsableTailoring,
            NombreProyecto: "App Productores");

    // Variante que controla el nombre del proyecto auditado (para los tests de
    // proyecto-match en HallazgosIdentidad).
    public static DocumentosExtraidos DocsProyecto(
        string nombreProyecto, params ArtefactoExtraido[] artefactos) => new(
            AuditoriaId: 100,
            ProyectoId: 200,
            EtapaId: 300,
            ProcedimientoCodigo: "PR 11-13",
            ProcedimientoNombre: "Desarrollo de Software",
            Artefactos: artefactos,
            ResponsableTailoring: "Responsable Demo",
            NombreProyecto: nombreProyecto);

    public static ArtefactoExtraido Artefacto(
        int id,
        string nombre = "Artefacto X",
        string? codigo = null,
        ExigibilidadArtefacto exigibilidad = ExigibilidadArtefacto.Exigible,
        ObligatoriedadArtefacto obligatoriedad = ObligatoriedadArtefacto.Mandatorio,
        EstadoAplicacionTailoring tailoring = EstadoAplicacionTailoring.Aplica,
        string? justificacionNoAplica = null,
        EstadoDisponibilidad disponibilidad = EstadoDisponibilidad.Encontrado,
        string? urlReferencia = null,
        DocumentoEncontrado? doc = null,
        IReadOnlyList<SeccionDetectada>? seccionesDetectadas = null,
        IReadOnlyList<SeccionDetectada>? seccionesTemplate = null,
        string? descripcion = null,
        DateOnly? vigenciaDetectada = null,
        DateOnly? vigenciaEsperada = null,
        bool documentoParseable = false,
        string? codigoDetectado = null,
        string? proyectoDetectado = null) => new(
            ArtefactoEsperadoId: id,
            CodigoArtefacto: codigo,
            NombreArtefacto: nombre,
            EtapaArtefactoId: 1,
            Exigibilidad: exigibilidad,
            Obligatoriedad: obligatoriedad,
            EstadoAplicacionTailoring: tailoring,
            JustificacionNoAplica: justificacionNoAplica,
            EstadoDisponibilidad: disponibilidad,
            UrlReferencia: urlReferencia,
            NombreTemplateArchivo: null,
            DocumentoEncontrado: doc,
            SeccionesDetectadas: seccionesDetectadas ?? [],
            SeccionesTemplate: seccionesTemplate ?? [],
            Descripcion: descripcion,
            VigenciaDetectada: vigenciaDetectada,
            VigenciaEsperada: vigenciaEsperada,
            DocumentoParseable: documentoParseable,
            CodigoDetectado: codigoDetectado,
            ProyectoDetectado: proyectoDetectado);

    public static DocumentoEncontrado Doc(
        string nombre,
        FuenteDocumento fuente = FuenteDocumento.Drive) =>
        new(nombre, fuente, "hash0000000000000000000000000000000000000000000000000000000000000");

    public static SeccionDetectada Sec(string titulo, bool tieneContenido) =>
        new(titulo, tieneContenido);

    public static HallazgoPreliminar Preliminar(
        int artefactoId,
        OrigenRegla origen,
        string descripcion = "Descripción del hallazgo",
        string justificacion = "Justificación del hallazgo") =>
        new(artefactoId, descripcion, justificacion, origen);

    public static HallazgosPreliminares Lote(
        AgenteOrigen origen,
        params HallazgoPreliminar[] hallazgos) =>
        new(AuditoriaId: 100, AgenteOrigen: origen, Hallazgos: hallazgos);
}
