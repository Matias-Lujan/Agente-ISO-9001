// ============================================================================
//  ContextoAuditoria — Contrato del workflow (contratos_agentes.md v2.2)
// ----------------------------------------------------------------------------
//  DTO que produce el nodo determinista ResolutorContexto y consume el agente
//  DocumentAnalysis. Reemplaza la arista directa que en v2.1 iba de
//  IniciarAuditoriaWorkflowInput a DocumentAnalysis.
//
//  Razón de ser: DocumentAnalysis es un agente LLM. No debe tocar la BD ni
//  ejecutar cálculos deterministas (comparar orden de etapas, combinar tipo
//  de proyecto con flags de obligatoriedad, concatenar rutas). Todo eso lo
//  precalcula ResolutorContexto y lo entrega ya resuelto en este DTO.
//
//  Coherente con el principio del proyecto: el plumbing del workflow es
//  código determinista, no razonamiento de LLM (mismo criterio que justifica
//  ConsolidadorResultado).
// ============================================================================

namespace ISOAuditAgent.API.Agents.Contracts;

// ----------------------------------------------------------------------------
//  DTO principal
// ----------------------------------------------------------------------------

/// <summary>
/// Contexto completo de una auditoría, listo para que DocumentAnalysis trabaje
/// sin consultar la BD. Lo produce ResolutorContexto.
/// </summary>
public sealed record ContextoAuditoria(
    int AuditoriaId,
    int ProyectoId,
    int EtapaId,
    IntegracionesProyecto Integraciones,
    IReadOnlyList<ArtefactoEsperadoContexto> ArtefactosEsperados
);

/// <summary>
/// IDs de las integraciones externas del proyecto. Cada uno es nullable: un
/// proyecto puede no usar todas las herramientas (modelo_bd.md, tabla
/// Proyecto). DocumentAnalysis usa estos IDs para resolver a qué fuente MCP
/// ir a buscar cada artefacto.
/// </summary>
public sealed record IntegracionesProyecto(
    string? DriveFolderId,
    string? TrelloBoardId,
    string? ClockifyProjectId
);

/// <summary>
/// Un artefacto que el procedimiento espera, con todos los cálculos
/// deterministas YA resueltos por ResolutorContexto.
/// </summary>
public sealed record ArtefactoEsperadoContexto(
    int ArtefactoEsperadoId,
    string? CodigoArtefacto,         // "FR 30", "FR 11"... null si no es FR formal
    string NombreArtefacto,          // "ERS", "Cronograma", "Casos de Prueba"
    int EtapaArtefactoId,            // etapa a la que pertenece este artefacto

    // --- Cálculos precalculados por ResolutorContexto ---

    /// <summary>
    /// Exigible vs PendienteEtapaFutura. Resuelto comparando el 'orden' de la
    /// etapa del artefacto contra el 'orden' de la etapa auditada:
    /// orden_artefacto &lt;= orden_auditada -> Exigible; si no -> PendienteEtapaFutura.
    /// </summary>
    ExigibilidadArtefacto Exigibilidad,

    /// <summary>
    /// Mandatorio vs EvaluarYJustificar. Resuelto combinando el tipo del
    /// proyecto (A/B) con los flags mandatorio_tipo_a / mandatorio_tipo_b del
    /// ArtefactoEsperado.
    /// </summary>
    ObligatoriedadArtefacto Obligatoriedad,

    /// <summary>
    /// Ruta absoluta al archivo de template, lista para abrir. Resuelta
    /// concatenando path_carpeta_templates (ConfiguracionSistema) con
    /// path_template_relativo del artefacto.
    /// null cuando el artefacto no tiene template (ej. tarjeta de Trello,
    /// registro de Clockify).
    /// </summary>
    string? PathTemplateAbsoluto
);

// ----------------------------------------------------------------------------
//  Enums — reutilizados idénticos de contratos_agentes.md (contrato 2)
// ----------------------------------------------------------------------------
//  Se referencian acá para documentar el contrato. En el código real viven
//  una sola vez en el namespace de contratos compartido; no se duplican.
// ----------------------------------------------------------------------------

public enum ExigibilidadArtefacto
{
    Exigible,
    PendienteEtapaFutura
}

public enum ObligatoriedadArtefacto
{
    Mandatorio,
    EvaluarYJustificar
}
