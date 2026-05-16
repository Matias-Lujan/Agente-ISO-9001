namespace AgenteAuditoria.Models;

/// <summary>
/// Tipo formal de hallazgo — lo asigna FindingsClassification.
/// Abreviatura corta según contratos v2.1.
/// </summary>
public enum TipoHallazgo
{
    NC,   // No Conformidad — incumplimiento directo del PR 11-13
    OBS,  // Observación — riesgo potencial sin evidencia directa
    OM    // Oportunidad de Mejora — sugerencia sin incumplimiento
}

/// <summary>
/// Fuente de origen de un documento analizado.
/// </summary>
public enum FuenteDocumento
{
    Drive,
    Trello,
    Clockify,
    MSProject
}

/// <summary>
/// Agente que detectó el hallazgo preliminar.
/// Se preserva en HallazgoClasificado para trazabilidad.
/// </summary>
public enum AgenteOrigen
{
    ComplianceValidation,
    ConsistencyVerification
}

/// <summary>
/// Origen de la regla incumplida.
/// FindingsClassification lo usa para aplicar: "si no está en Procedimiento → máximo OM".
/// No se propaga al output — cumplió su rol en la clasificación.
/// </summary>
public enum OrigenRegla
{
    Procedimiento,
    Template,
    Tailoring
}