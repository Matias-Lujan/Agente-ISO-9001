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
    string Estado
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