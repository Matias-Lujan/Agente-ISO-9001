
// ============================================================================
//  Contrato 4 — Salida de ComplianceValidation y ConsistencyVerification
//  (contratos_agentes.md v2.2)
// ----------------------------------------------------------------------------
//  Lo producen ComplianceValidation y ConsistencyVerification, cada uno por
//  separado. Cada instancia del DTO corresponde a un solo agente; el
//  AgenteOrigen va en la raíz, no por hallazgo.
//
//  Lo consume FindingsClassification, que recibe los dos lotes por separado
//  (fan-in barrier de MAF) y los distingue por su AgenteOrigen.
//
//  Invariantes (contratos_agentes.md, contrato 4) — las respetan los agentes
//  al construir el DTO, no se codifican en el record:
//   - ArtefactoEsperadoId debe corresponder a un artefacto presente en el
//     DocumentosExtraidos recibido por el validador.
//   - Los hallazgos solo se generan sobre artefactos exigibles.
//   - Descripcion y Justificacion son requeridos (no null, no vacío).
//   - Lista Hallazgos vacía es válida: validación sin hallazgos.
//
//  Enums:
//   - OrigenRegla: propio del workflow, se define acá.
//   - AgenteOrigen: enum compartido con las entidades EF, vive en
//     ISOAuditAgent.API.Models.
// ============================================================================

using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.Contracts;

// ----------------------------------------------------------------------------
//  DTO principal
// ----------------------------------------------------------------------------

/// <summary>
/// Lote de hallazgos preliminares producido por un validador. Los validadores
/// detectan problemas pero NO clasifican gravedad (NC/OBS/OM): eso lo hace
/// FindingsClassification.
/// </summary>
public sealed record HallazgosPreliminares(
    int AuditoriaId,
    AgenteOrigen AgenteOrigen,
    IReadOnlyList<HallazgoPreliminar> Hallazgos
);

/// <summary>
/// Un problema detectado sobre un artefacto. Descripcion: qué se detectó.
/// Justificacion: por qué es un problema y qué regla se incumple.
/// </summary>
public sealed record HallazgoPreliminar(
    int ArtefactoEsperadoId,
    string Descripcion,
    string Justificacion,
    OrigenRegla OrigenRegla
);

// ----------------------------------------------------------------------------
//  Enum propio del workflow — definido una sola vez, acá
// ----------------------------------------------------------------------------

/// <summary>
/// De dónde sale la regla incumplida. FindingsClassification lo usa para
/// aplicar la regla "si no está escrito en el procedimiento -> como mucho OM".
/// </summary>
public enum OrigenRegla
{
    Procedimiento,
    Template,
    Tailoring
}