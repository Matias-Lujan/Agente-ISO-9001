namespace AgenteAuditoria.Models;

/// <summary>
/// Contrato 3 — Output de ComplianceValidation y ConsistencyVerification.
/// Input de FindingsClassificationExecutor.
///
/// Llega DOS VECES al agente de clasificación (una por cada validador).
/// MAF acumula los dos lotes y procesa cuando tiene ambos (fan-in barrier).
///
/// CAMBIOS respecto a versión anterior:
/// - Ya no tiene ProcesoId ni EtapaActual — la etapa viaja en el Contrato 1
///   y los validadores no la repiten.
/// - Ya no tiene EvidenciaHallazgo — los validadores no recolectan evidencias
///   textuales; el DocumentosExtraidos ya tiene toda la info del artefacto.
/// - ArtefactoEsperadoId reemplaza a la descripción libre como llave de unión.
/// - OrigenRegla indica de dónde sale la regla incumplida.
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
    /// <summary>
    /// ID del artefacto esperado al que refiere el hallazgo.
    /// Debe corresponder a un artefacto presente en el DocumentosExtraidos
    /// que recibió el validador. Es la llave que une el hallazgo con su contexto.
    /// </summary>
    int ArtefactoEsperadoId,

    /// <summary>Descripción del problema detectado, en lenguaje natural.</summary>
    string Descripcion,

    /// <summary>
    /// Justificación de por qué es un hallazgo — explica la regla y la evidencia.
    /// No referencia la cláusula exacta del procedimiento; usa lenguaje natural.
    /// </summary>
    string Justificacion,

    /// <summary>
    /// De dónde viene la regla que se está incumpliendo.
    /// FindingsClassification aplica: "si OrigenRegla != Procedimiento → máximo OM".
    /// </summary>
    OrigenRegla OrigenRegla
);