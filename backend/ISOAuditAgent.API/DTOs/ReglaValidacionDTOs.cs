namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Como se devuelve una regla de validacion al frontend.
/// </summary>
public record ReglaValidacionResponse(
    int Id,
    int ProcedimientoId,
    int? EtapaId,
    string? Codigo,
    string Descripcion,
    string Tipo,
    bool Obligatoria,
    bool Activa
);

/// <summary>
/// Lo que se manda para crear una regla nueva.
/// Solo el Administrador puede crear reglas — cumple RF-04.
/// </summary>
public record CrearReglaRequest(
    int ProcedimientoId,
    int? EtapaId,
    string? Codigo,
    string Descripcion,
    string Tipo,
    bool Obligatoria
);

/// <summary>
/// Lo que se manda para modificar una regla existente.
/// Todos los campos son opcionales — solo se actualizan los que vienen con valor.
/// </summary>
public record ModificarReglaRequest(
    string? Codigo,
    string? Descripcion,
    string? Tipo,
    bool? Obligatoria,
    bool? Activa
);