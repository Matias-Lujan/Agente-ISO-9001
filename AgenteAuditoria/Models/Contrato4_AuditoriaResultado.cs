namespace AgenteAuditoria.Models;

/// <summary>
/// Contrato 4 — Output de FindingsClassificationExecutor.
///
/// IMPORTANTE: este ya NO es AuditoriaResultado.
/// FindingsClassification produce HallazgosClasificados.
/// El AuditoriaResultado (Contrato 5) lo arma el ConsolidadorResultado
/// — un nodo determinista sin LLM que vive en el orquestador.
///
/// Invariantes según contratos v2.1:
/// - Cada HallazgoClasificado corresponde 1 a 1 con un HallazgoPreliminar recibido.
///   FindingsClassification clasifica pero NO inventa, omite ni fusiona hallazgos.
/// - AgenteOrigen se preserva del HallazgoPreliminar original.
/// - Lista Hallazgos vacía es válida (auditoría sin hallazgos).
/// - OrigenRegla del preliminar se consumió en la clasificación y NO se propaga.
/// </summary>
public sealed record HallazgosClasificados(
    int AuditoriaId,
    IReadOnlyList<HallazgoClasificado> Hallazgos
);

/// <summary>
/// Hallazgo con clasificación formal NC/OBS/OM.
/// Se reutiliza idéntico en el AuditoriaResultado (Contrato 5).
/// </summary>
public sealed record HallazgoClasificado(
    /// <summary>
    /// Llave que une el hallazgo con el ArtefactoEvaluado.
    /// Viene del HallazgoPreliminar — se propaga sin modificar.
    /// </summary>
    int ArtefactoEsperadoId,

    /// <summary>Clasificación formal asignada por FindingsClassification vía Gemini.</summary>
    TipoHallazgo Tipo,

    string Descripcion,
    string Justificacion,

    /// <summary>
    /// Qué validador detectó el hallazgo — viene del HallazgoPreliminar.
    /// Acá baja a nivel de hallazgo (a diferencia del Contrato 3 donde
    /// estaba en la raíz) porque los hallazgos viajan en lista plana.
    /// </summary>
    AgenteOrigen AgenteOrigen
);