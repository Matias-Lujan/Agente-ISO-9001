namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Lo que se manda para crear un proyecto nuevo.
/// </summary>
public record CrearProyectoRequest(
    string Nombre,
    string? Descripcion,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    string TipoProyecto,        // "A" o "B"
    int? HorasEstimadas,
    int ProcedimientoId,
    // Integraciones externas — todas opcionales
    string? TrelloBoardId,
    string? ClockifyProjectId,
    string? DriveFolderId
);

/// <summary>
/// Lo que se manda para modificar un proyecto existente.
/// Todos los campos son opcionales — solo se actualizan los que vienen con valor.
/// </summary>
public record ModificarProyectoRequest(
    string? Nombre,
    string? Descripcion,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    string? TipoProyecto,
    int? HorasEstimadas,
    string? TrelloBoardId,
    string? ClockifyProjectId,
    string? DriveFolderId,
    bool? Activo
);

/// <summary>
/// Como se devuelve un proyecto al frontend.
/// </summary>
public record ProyectoResponse(
    int Id,
    string Nombre,
    string? Descripcion,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    string TipoProyecto,
    int? HorasEstimadas,
    int ProcedimientoId,
    string? TrelloBoardId,
    string? ClockifyProjectId,
    string? DriveFolderId,
    bool Activo,
    // Lista de usuarios asignados al proyecto
    List<UsuarioResponse> Responsables
);

/// <summary>
/// Lo que se manda para asignar un usuario a un proyecto.
/// </summary>
public record AsignarResponsableRequest(int UsuarioId);