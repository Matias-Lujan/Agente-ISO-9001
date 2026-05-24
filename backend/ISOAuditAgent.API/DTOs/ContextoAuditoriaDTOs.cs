namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Contrato de salida del ResolutorContexto (Nodo 1 del workflow).
/// Es todo el contexto que necesitan los agentes para ejecutar la auditoria.
/// Lo produce el ResolutorContexto y lo consumen:
///   - DocumentAnalysis (Nodo 2)
///   - ComplianceValidation (Nodo 3)
///   - ConsistencyVerification (Nodo 4)
///   - FindingsClassification (Nodo 5)
/// </summary>
public record ContextoAuditoria(
    // Identificadores principales
    int AuditoriaId,
    int ProyectoId,
    int EtapaId,
    int UsuarioId,

    // Datos del proyecto
    string NombreProyecto,
    string TipoProyecto,        // "A" o "B" — determina que artefactos son obligatorios
    int? HorasEstimadas,
    int ProcedimientoId,

    // IDs de integraciones externas — los usa DocumentAnalysis para buscar documentos
    string? TrelloBoardId,
    string? ClockifyProjectId,
    string? DriveFolderId,

    // Datos de la etapa que se esta auditando
    string NombreEtapa,
    int OrdenEtapa,

    // Artefactos que se esperan en esta etapa segun el procedimiento
    // DocumentAnalysis los usa para saber que buscar
    List<ArtefactoContextoDto> ArtefactosEsperados,

    // Reglas de validacion que aplican a esta etapa
    // Los agentes las usan para saber que validar
    List<ReglaContextoDto> ReglasValidacion
);

/// <summary>
/// Un artefacto esperado dentro del contexto de auditoria.
/// Version simplificada de ArtefactoEsperadoResponse — solo los campos
/// que necesitan los agentes.
/// </summary>
public record ArtefactoContextoDto(
    int Id,
    string? Codigo,
    string Nombre,
    // Si es obligatorio segun el tipo de proyecto (A o B)
    bool EsObligatorio,
    // Ruta al template — DocumentAnalysis la usa para comparar estructura
    string? PathTemplateRelativo
);

/// <summary>
/// Una regla de validacion dentro del contexto de auditoria.
/// Version simplificada de ReglaValidacionResponse — solo los campos
/// que necesitan los agentes.
/// </summary>
public record ReglaContextoDto(
    int Id,
    string? Codigo,
    string Descripcion,
    string Tipo,
    bool Obligatoria
);

/// <summary>
/// Input que recibe el endpoint del ResolutorContexto.
/// Son los tres datos que ingresa el auditor al iniciar una auditoria.
/// </summary>
public record IniciarAuditoriaRequest(
    int ProyectoId,
    int EtapaId
);