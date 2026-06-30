
// ============================================================================
//  Contrato 4 � Salida de ComplianceValidation y ConsistencyVerification
//  (contratos_agentes.md v2.2)
// ----------------------------------------------------------------------------
//  Lo producen ComplianceValidation y ConsistencyVerification, cada uno por
//  separado. Cada instancia del DTO corresponde a un solo agente; el
//  AgenteOrigen va en la ra�z, no por hallazgo.
//
//  Lo consume FindingsClassification, que recibe los dos lotes por separado
//  (fan-in barrier de MAF) y los distingue por su AgenteOrigen.
//
//  Invariantes (contratos_agentes.md, contrato 4) � las respetan los agentes
//  al construir el DTO, no se codifican en el record:
//   - ArtefactoEsperadoId debe corresponder a un artefacto presente en el
//     DocumentosExtraidos recibido por el validador.
//   - Los hallazgos solo se generan sobre artefactos exigibles.
//   - Descripcion y Justificacion son requeridos (no null, no vac�o).
//   - Lista Hallazgos vac�a es v�lida: validaci�n sin hallazgos.
//
//  Enums:
//   - OrigenRegla: propio del workflow, se define ac�.
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
/// Un problema detectado sobre un artefacto. Descripcion: qu� se detect�.
/// Justificacion: por qu� es un problema y qu� regla se incumple.
/// </summary>
public sealed record HallazgoPreliminar(
    int ArtefactoEsperadoId,
    string Descripcion,
    string Justificacion,
    OrigenRegla OrigenRegla
);

// ----------------------------------------------------------------------------
//  Enum propio del workflow � definido una sola vez, ac�
// ----------------------------------------------------------------------------

/// <summary>
/// De d�nde sale la regla incumplida. FindingsClassification lo usa para
/// aplicar la regla "si no est� escrito en el procedimiento -> como mucho OM".
/// </summary>
public enum OrigenRegla
{
    Procedimiento,
    Template,
    Tailoring,

    /// <summary>
    /// Desvío de vigencia/versionado del formulario: la vigencia detectada en el
    /// documento no coincide con la vigente en la referencia de Calidad. Techo de
    /// clasificación OBS (lo fuerza ClasificacionResponseParser): usar un
    /// formulario desactualizado es un desvío formal, no incumplimiento de fondo.
    /// </summary>
    Vigencia
}