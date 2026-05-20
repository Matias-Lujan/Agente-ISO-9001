using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.ConsistencyVerification.Contracts;

// =============================================================================
// CONTRATO 2 — Input que llega desde DocumentAnalysis
// DocumentAnalysis ya fue a buscar los documentos.
// Nosotros recibimos la lista lista para analizar.
//
// NOTA: Estos tipos viven en un namespace propio del agente
// (Agents.ConsistencyVerification.Contracts) para no colisionar con los DTOs
// homónimos que ya existían en ISOAuditAgent.API.DTOs (firmas incompatibles).
// =============================================================================

/// <summary>
/// Input principal del agente. Llega desde DocumentAnalysis
/// con la lista de artefactos ya buscados y clasificados.
/// </summary>
public sealed record DocumentosExtraidos(
    int AuditoriaId,
    int ProyectoId,
    int EtapaId,
    IReadOnlyList<ArtefactoExtraido> Artefactos
);

/// <summary>
/// Un artefacto (documento) que DocumentAnalysis encontro o no encontro.
/// Contiene todo lo que nuestro agente necesita para analizar consistencia.
/// </summary>
public sealed record ArtefactoExtraido(
    int ArtefactoEsperadoId,
    string? CodigoArtefacto,
    string NombreArtefacto,
    int EtapaArtefactoId,

    // Exigible = hay que revisarlo ahora.
    // PendienteEtapaFutura = todavia no corresponde a esta etapa.
    ExigibilidadArtefacto Exigibilidad,

    // Mandatorio = siempre obligatorio.
    // EvaluarYJustificar = en proyectos tipo B puede no generarse con justificacion.
    ObligatoriedadArtefacto Obligatoriedad,

    // Viene del tailoring del proyecto.
    EstadoTailoring EstadoTailoring,

    // Si el tailoring dice NoAplica, aca viene la razon.
    string? JustificacionNoAplica,

    // Resultado de la busqueda que hizo DocumentAnalysis.
    EstadoDisponibilidad EstadoDisponibilidad,

    string? UrlReferencia,

    // Ruta al template que corresponde a este artefacto.
    // Ya viene resuelta por DocumentAnalysis — no hay que calcularla.
    string? PathTemplateAbsoluto,

    // El archivo encontrado. Es null si EstadoDisponibilidad != Encontrado.
    DocumentoEncontrado? DocumentoEncontrado,

    // Secciones que DocumentAnalysis detecto dentro del documento.
    // Lista vacia si no aplica template (ej: registro de Clockify, tarjeta de Trello).
    IReadOnlyList<SeccionDetectada> SeccionesDetectadas
);

/// <summary>
/// Informacion del archivo encontrado por DocumentAnalysis.
/// </summary>
public sealed record DocumentoEncontrado(
    string NombreArchivo,
    FuenteDocumento Fuente,
    string HashContenido
);

/// <summary>
/// Una seccion detectada dentro de un documento.
/// TieneContenido = false significa que la seccion existe pero esta vacia.
/// </summary>
public sealed record SeccionDetectada(
    string Titulo,
    bool TieneContenido
);

// ── Enums del Contrato 2 ──────────────────────────────────────────────────────

public enum ExigibilidadArtefacto
{
    Exigible,
    PendienteEtapaFutura
}

public enum ObligatoriedadArtefacto
{
    Mandatorio,
    EvaluarYJustificar
}

public enum EstadoTailoring
{
    Aplica,
    NoAplica,
    SinDeclararEnTailoring
}

public enum EstadoDisponibilidad
{
    Encontrado,
    Faltante,
    NoBuscado
}

public enum FuenteDocumento
{
    Drive,
    Trello,
    Clockify,
    MSProject
}

// =============================================================================
// CONTRATO 3 — Output de nuestro agente (HallazgosPreliminares)
// Esto es lo que le entregamos a FindingsClassification.
// Solo detectamos problemas — NO los clasificamos en NC/OBS/OM.
// Eso le corresponde a FindingsClassification.
// =============================================================================

/// <summary>
/// Output principal del agente. Lista de problemas detectados
/// sobre estructura y consistencia de los documentos.
/// FindingsClassification los recibira y clasificara en NC/OBS/OM.
/// </summary>
public sealed record HallazgosPreliminares(
    int AuditoriaId,
    AgenteOrigen AgenteOrigen,
    IReadOnlyList<HallazgoPreliminar> Hallazgos
);

/// <summary>
/// Un problema detectado sobre un artefacto.
/// </summary>
public sealed record HallazgoPreliminar(
    // ID del artefacto con problema. Debe existir en el DocumentosExtraidos recibido.
    int ArtefactoEsperadoId,
    // Que problema tiene. Ej: "El ERS no tiene la seccion Alcance con contenido".
    string Descripcion,
    // Por que es un problema. Ej: "El template del ERS requiere la seccion Alcance".
    string Justificacion,
    // De donde viene la regla incumplida.
    OrigenRegla OrigenRegla
);

public enum OrigenRegla
{
    Procedimiento,  // La regla viene del procedimiento PR 11-13 de BDT
    Template,       // La regla viene del template del artefacto
    Tailoring       // La regla viene del tailoring del proyecto
}

// ── Interno — solo para manejo de errores dentro del agente ──────────────────
public enum AgentStatus { Success, PartialSuccess, Failed }
