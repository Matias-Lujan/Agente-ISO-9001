// ============================================================================
//  Contrato 6 — Output final del workflow (contratos_agentes.md v2.2)
// ----------------------------------------------------------------------------
//  Escrito desde la especificación contratos_agentes.md v2.2 — no proviene de
//  ningún diff de los agentes.
//
//  Lo produce el nodo determinista ConsolidadorResultado: recibe
//  ResultadoClasificacionConContexto (entrada única) y arma el resultado
//  final a partir del DocumentosExtraidos y los HallazgosClasificados que
//  vienen adentro.
//
//  Lo consume la API REST (vía el background worker y
//  AuditoriaPersistenceService): persiste todo en transacción única y marca
//  la auditoría como Completada.
//
//  ENUMS — los enums de este contrato son COMPARTIDOS con las entidades EF y
//  se definen UNA SOLA VEZ en ISOAuditAgent.API.Models:
//   - EstadoAplicacionTailoring  (lo crea el Paso 3 de la integración)
//   - ResultadoEvaluacion        (ya existe en dev/Models)
//   - TipoHallazgo               (ya existe en dev/Models)
//   - FuenteDocumento            (ya existe en dev/Models)
//   - AgenteOrigen               (ya existe en dev/Models)
//  La spec v2.2 los muestra inline en el bloque del contrato 6, pero sus
//  propias "Decisiones de diseño" indican definirlos en un namespace común.
//  La tarea 9.2.10 del plan maestro resuelve la contradicción: viven en
//  Models. Acá solo se referencian.
//
//  Tres listas paralelas, no jerarquía anidada: mapean directo con las 3
//  tablas de BD. ArtefactoEsperadoId es la llave de unión entre listas.
//
//  Invariantes (contratos_agentes.md, contrato 6) — los verifica el nodo
//  ConsolidadorResultado al ensamblar, no se codifican en los records:
//   - EstadoAplicacionTailoring = Aplica -> JustificacionNoAplica es null.
//   - NoAplica + justificación con texto -> Resultado = NoAplica.
//   - NoAplica sin justificación -> Resultado = NoConforme con hallazgo.
//   - SinDeclararEnTailoring -> Resultado = NoConforme con hallazgo.
//   - Resultado PendienteEtapaFutura o NoAplica -> sin hallazgos ni
//     documentos asociados a ese ArtefactoEsperadoId.
//   - Resultado NoConforme -> al menos un hallazgo asociado.
//   - Los ArtefactoEsperadoId de Hallazgos y DocumentosAnalizados deben
//     existir en ArtefactosEvaluados.
// ============================================================================

using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.Contracts;

/// <summary>
/// Resultado final del workflow de auditoría. Tres listas paralelas unidas
/// por ArtefactoEsperadoId. La API REST lo persiste en transacción única.
/// </summary>
public sealed record AuditoriaResultado(
    int AuditoriaId,
    IReadOnlyList<ArtefactoEvaluadoResultado> ArtefactosEvaluados,
    IReadOnlyList<HallazgoClasificado> Hallazgos,
    IReadOnlyList<DocumentoAnalizadoResultado> DocumentosAnalizados
);

/// <summary>
/// Un artefacto evaluado en la auditoría: qué dijo el tailoring sobre él
/// (EstadoAplicacionTailoring) y cuál fue el resultado de la evaluación
/// (Resultado).
/// </summary>
public sealed record ArtefactoEvaluadoResultado(
    int ArtefactoEsperadoId,
    EstadoAplicacionTailoring EstadoAplicacionTailoring,
    string? JustificacionNoAplica,
    ResultadoEvaluacion Resultado,
    string? Observaciones
);

/// <summary>
/// Un hallazgo ya clasificado en NC/OBS/OM. Se reutiliza idéntico en el
/// contrato 5 (HallazgosClasificados.Hallazgos).
/// </summary>
public sealed record HallazgoClasificado(
    int ArtefactoEsperadoId,
    TipoHallazgo Tipo,
    string Descripcion,
    string Justificacion,
    AgenteOrigen AgenteOrigen
);

/// <summary>
/// Un documento o registro analizado durante la auditoría, con su origen y
/// su hash de contenido para trazabilidad.
/// </summary>
public sealed record DocumentoAnalizadoResultado(
    int ArtefactoEsperadoId,
    string NombreArchivo,
    FuenteDocumento Fuente,
    string? UrlReferencia,
    string HashContenido
);