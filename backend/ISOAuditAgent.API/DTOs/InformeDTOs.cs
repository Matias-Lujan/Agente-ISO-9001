namespace ISOAuditAgent.API.DTOs;

public record GenerarInformeRequest(int AuditoriaId);

public record InformeResponse(
    int Id,
    int AuditoriaId,
    string NombreProyecto,
    DateTime FechaGeneracion,
    string Tipo,
    string Contenido
);