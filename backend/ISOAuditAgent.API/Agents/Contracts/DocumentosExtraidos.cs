// ============================================================================
//  Contrato 3 — Salida de DocumentAnalysis (contratos_agentes.md v2.2)
// ----------------------------------------------------------------------------
//  Lo produce DocumentAnalysis: recibe el ContextoAuditoria (artefactos
//  esperados ya enriquecidos), lee el tailoring del proyecto, lo cruza con
//  los artefactos esperados y va a buscar los exigibles que aplican.
//
//  Lo consumen ComplianceValidation y ConsistencyVerification en paralelo
//  (fan-out), y también FindingsClassification vía edge directo.
//
//  DESVÍO REGISTRADO respecto de contratos_agentes.md v2.2:
//  La spec v2.2 define el campo de ArtefactoExtraido como
//  'EstadoTailoring EstadoTailoring', con un enum propio EstadoTailoring.
//  Por la decisión cerrada 2.8 del plan maestro de integración, el enum
//  EstadoTailoring se ELIMINA y se unifica en el enum compartido
//  EstadoAplicacionTailoring (definido en ISOAuditAgent.API.Models, junto a
//  las entidades EF). El campo pasa a 'EstadoAplicacionTailoring
//  EstadoAplicacionTailoring'. La spec v2.2 queda desactualizada en este
//  punto.
//
//  Enums:
//   - EstadoDisponibilidad: propio del workflow, se define acá.
//   - ExigibilidadArtefacto / ObligatoriedadArtefacto: definidos en
//     ContextoAuditoria.cs (contrato 2), se referencian.
//   - EstadoAplicacionTailoring / FuenteDocumento: enums compartidos con las
//     entidades EF, viven en ISOAuditAgent.API.Models.
// ============================================================================

using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.Contracts;

// ----------------------------------------------------------------------------
//  DTO principal
// ----------------------------------------------------------------------------

/// <summary>
/// Salida de DocumentAnalysis. Incluye TODOS los artefactos esperados del
/// procedimiento — exigibles y PendienteEtapaFutura — para que el dashboard
/// pueda calcular el porcentaje de cumplimiento sobre el total.
/// </summary>
public sealed record DocumentosExtraidos(
    int AuditoriaId,
    int ProyectoId,
    int EtapaId,
    IReadOnlyList<ArtefactoExtraido> Artefactos
);

/// <summary>
/// Resultado por artefacto esperado del procedimiento: clasificado por
/// exigibilidad y obligatoriedad y, si aplica, con el documento físico
/// encontrado en la fuente externa.
/// </summary>
public sealed record ArtefactoExtraido(
    int ArtefactoEsperadoId,
    string? CodigoArtefacto,
    string NombreArtefacto,
    int EtapaArtefactoId,

    // Pre-resueltos por ResolutorContexto; DocumentAnalysis los propaga.
    ExigibilidadArtefacto Exigibilidad,
    ObligatoriedadArtefacto Obligatoriedad,

    // Voz del tailoring. Enum unificado (ver desvío en la cabecera).
    EstadoAplicacionTailoring EstadoAplicacionTailoring,
    string? JustificacionNoAplica,

    // Resultado de la recolección física del artefacto.
    EstadoDisponibilidad EstadoDisponibilidad,
    string? UrlReferencia,
    string? PathTemplateAbsoluto,
    DocumentoEncontrado? DocumentoEncontrado,
    IReadOnlyList<SeccionDetectada> SeccionesDetectadas
);

/// <summary>
/// Documento físico efectivamente recolectado desde una fuente externa.
/// Solo se construye cuando EstadoDisponibilidad = Encontrado.
/// El HashContenido se calcula sobre los bytes (fuentes binarias) o sobre el
/// texto canónico normalizado (fuentes sin binario).
/// </summary>
public sealed record DocumentoEncontrado(
    string NombreArchivo,
    FuenteDocumento Fuente,
    string HashContenido
);

/// <summary>
/// Sección detectada dentro del documento encontrado, comparable contra el
/// template del FR correspondiente. TieneContenido indica si la sección tiene
/// algún contenido más allá del propio título.
/// </summary>
public sealed record SeccionDetectada(
    string Titulo,
    bool TieneContenido
);

// ----------------------------------------------------------------------------
//  Enum propio del workflow — definido una sola vez, acá
// ----------------------------------------------------------------------------

/// <summary>
/// Resultado de la recolección física del artefacto. Solo los artefactos
/// exigibles que aplican se buscan; el resto queda en NoBuscado.
/// </summary>
public enum EstadoDisponibilidad
{
    Encontrado,
    Faltante,
    NoBuscado
}