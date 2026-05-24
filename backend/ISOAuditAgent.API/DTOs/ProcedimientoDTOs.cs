namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Como se devuelve un procedimiento al frontend.
/// </summary>
public record ProcedimientoResponse(
    int Id,
    string Codigo,
    string Nombre,
    string? Descripcion
);

/// <summary>
/// Como se devuelve una etapa al frontend.
/// </summary>
public record EtapaResponse(
    int Id,
    int ProcedimientoId,
    string Nombre,
    int Orden,
    string? Descripcion,
    // Lista de artefactos esperados en esta etapa
    List<ArtefactoEsperadoResponse> Artefactos
);

/// <summary>
/// Como se devuelve un artefacto esperado al frontend.
/// </summary>
public record ArtefactoEsperadoResponse(
    int Id,
    int EtapaId,
    string? Codigo,
    string Nombre,
    string? Descripcion,
    bool MandatorioTipoA,
    bool MandatorioTipoB,
    string? PathTemplateRelativo
);

/// <summary>
/// Lo que se manda para crear un artefacto esperado nuevo.
/// Solo el Administrador puede gestionar artefactos.
/// </summary>
public record CrearArtefactoRequest(
    int EtapaId,
    string? Codigo,
    string Nombre,
    string? Descripcion,
    bool MandatorioTipoA,
    bool MandatorioTipoB,
    string? PathTemplateRelativo
);

/// <summary>
/// Lo que se manda para modificar un artefacto esperado.
/// Todos los campos son opcionales.
/// </summary>
public record ModificarArtefactoRequest(
    string? Codigo,
    string? Nombre,
    string? Descripcion,
    bool? MandatorioTipoA,
    bool? MandatorioTipoB,
    string? PathTemplateRelativo
);