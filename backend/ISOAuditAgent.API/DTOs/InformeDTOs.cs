namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Lo que se manda para generar un informe manual.
/// El informe automatico lo genera el sistema solo
/// cuando el workflow de agentes termina.
/// </summary>
public record GenerarInformeRequest(
    int AuditoriaId
);

/// <summary>
/// Como se devuelve un informe al frontend.
/// </summary>
public record InformeResponse(
    int Id,
    int AuditoriaId,
    string NombreProyecto,
    DateTime FechaGeneracion,
    string Tipo,
    string? Contenido,
    bool Activo
);