namespace ISOAuditAgent.API.DTOs;

public record CrearAuditoriaRequest(
    int ProyectoId,
    int EtapaId
);

public record AuditoriaResponse(
    int Id,
    int ProyectoId,
    string NombreProyecto,
    int UsuarioId,
    string NombreAuditor,
    int EtapaId,
    DateTime FechaInicioUtc,
    DateTime? FechaFinalizacionUtc,
    string Estado,
    // Resumen del fallo cuando Estado == Fallida (null en otros casos).
    string? CategoriaError,
    string? MensajeError
);

/// <summary>
/// Una entrada del log de errores de una auditoría. No incluye el detalle
/// técnico (stack): eso queda en la BD para el equipo, no se muestra al auditor.
/// </summary>
public record RegistroErrorResponse(
    int Id,
    string? Nodo,
    string Categoria,
    string Mensaje,
    DateTime FechaUtc
);

/// <summary>
/// Progreso de un nodo del workflow para el polling del frontend.
/// Nodo: DocumentAnalysis | ComplianceValidation | ConsistencyVerification | FindingsClassification
/// Estado: Pendiente | EnCurso | Completado | Fallido
/// </summary>
public record ProgresoNodoResponse(
    string Nodo,
    string Estado,
    DateTime? FechaInicioUtc,
    DateTime? FechaFinUtc
);

public record AuditoriaContadoresResponse(
    int ArtefactosEvaluados,
    int Hallazgos,
    int DocumentosAnalizados
);

public record HallazgoResultadoResponse(
    int Id,
    string Tipo,
    string Descripcion,
    string Justificacion,
    string AgenteOrigen
);

public record DocumentoAnalizadoResultadoResponse(
    int Id,
    string NombreArchivo,
    string Fuente,
    string? UrlReferencia,
    string HashContenido
);

public record ArtefactoEvaluadoResultadoResponse(
    int Id,
    int ArtefactoEsperadoId,
    string? ArtefactoEsperadoCodigo,
    string? ArtefactoEsperadoNombre,
    string Aplica,
    string? JustificacionNoAplica,
    string Resultado,
    string? Observaciones,
    IReadOnlyList<HallazgoResultadoResponse> Hallazgos,
    IReadOnlyList<DocumentoAnalizadoResultadoResponse> DocumentosAnalizados
);

public record AuditoriaResultadoResponse(
    int Id,
    int ProyectoId,
    int EtapaId,
    int UsuarioId,
    string Estado,
    DateTime FechaInicioUtc,
    DateTime? FechaFinalizacionUtc,
    AuditoriaContadoresResponse Contadores,
    IReadOnlyList<ArtefactoEvaluadoResultadoResponse> ArtefactosEvaluados
);