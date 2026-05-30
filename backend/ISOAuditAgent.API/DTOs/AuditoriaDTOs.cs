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