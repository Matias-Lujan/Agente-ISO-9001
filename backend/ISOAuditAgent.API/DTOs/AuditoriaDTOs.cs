namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Lo que se manda para crear una auditoria nueva.
/// El usuario elige el proyecto y la etapa a auditar.
/// </summary>
public record CrearAuditoriaRequest(
    int ProyectoId,
    int EtapaId
);

/// <summary>
/// Como se devuelve una auditoria al frontend.
/// </summary>
public record AuditoriaResponse(
    int Id,
    int ProyectoId,
    string NombreProyecto,
    int UsuarioId,
    string NombreAuditor,
    int EtapaId,
    DateTime FechaInicioUtc,
    DateTime? FechaFinalizacionUtc,
    int? HorasEstimadas,
    string Estado,
    bool Activo
);