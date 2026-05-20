using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.FindingsClassification.Contracts;

// =============================================================================
// CONTRATOS DEL AGENTE FindingsClassification
//
// Estos tipos viven en un namespace propio del agente
// (Agents.FindingsClassification.Contracts) para no colisionar con los DTOs
// homónimos que ya existían en ISOAuditAgent.API.DTOs ni con los del agente
// ConsistencyVerification (firmas incompatibles entre módulos).
//
// Los enums AgenteOrigen, TipoHallazgo y FuenteDocumento se reutilizan desde
// ISOAuditAgent.API.Models — los valores coinciden y son canónicos del API.
// =============================================================================

// ── Contrato 3 — Input ───────────────────────────────────────────────────────
// Output de ComplianceValidation y ConsistencyVerification.
// Llega DOS VECES al agente de clasificación (una por cada validador).
// MAF acumula los dos lotes y procesa cuando tiene ambos (fan-in barrier).
// El controller HTTP recibe la lista de los dos grupos en un solo request.

/// <summary>
/// Lote de hallazgos preliminares producido por un validador
/// (ComplianceValidation o ConsistencyVerification).
/// </summary>
public sealed record HallazgosPreliminares(
    int AuditoriaId,
    AgenteOrigen AgenteOrigen,
    IReadOnlyList<HallazgoPreliminar> Hallazgos
);

/// <summary>
/// Hallazgo detectado por un validador — sin clasificar todavía.
/// La clasificación (NC/OBS/OM) es responsabilidad exclusiva de FindingsClassification.
/// </summary>
public sealed record HallazgoPreliminar(
    int ArtefactoEsperadoId,
    string Descripcion,
    string Justificacion,
    OrigenRegla OrigenRegla
);

// ── Contrato 4 — Output ──────────────────────────────────────────────────────
// Lo que FindingsClassification entrega al ConsolidadorResultado.
//
// Invariantes (contratos v2.1):
// - Cada HallazgoClasificado corresponde 1 a 1 con un HallazgoPreliminar recibido.
// - AgenteOrigen se preserva del HallazgoPreliminar original.
// - Lista vacía es válida (auditoría sin hallazgos).
// - OrigenRegla del preliminar se consumió en la clasificación y NO se propaga.

public sealed record HallazgosClasificados(
    int AuditoriaId,
    IReadOnlyList<HallazgoClasificado> Hallazgos
);

public sealed record HallazgoClasificado(
    int ArtefactoEsperadoId,
    TipoHallazgo Tipo,
    string Descripcion,
    string Justificacion,
    AgenteOrigen AgenteOrigen
);

// ── Enums propios del agente ─────────────────────────────────────────────────

/// <summary>
/// Origen de la regla incumplida.
/// FindingsClassification aplica: "si OrigenRegla != Procedimiento → máximo OM".
/// No se propaga al output — cumplió su rol en la clasificación.
/// </summary>
public enum OrigenRegla
{
    Procedimiento,
    Template,
    Tailoring
}
