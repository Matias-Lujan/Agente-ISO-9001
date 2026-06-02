// ============================================================================
//  Contrato 2 � Salida de ResolutorContexto (contratos_agentes.md v2.2)
// ----------------------------------------------------------------------------
//  DTO que produce el nodo determinista ResolutorContexto y consume el agente
//  DocumentAnalysis. Reemplaza la arista directa que en v2.1 iba de
//  IniciarAuditoriaWorkflowInput a DocumentAnalysis.
//
//  Raz�n de ser: DocumentAnalysis es un agente LLM. No debe tocar la BD ni
//  ejecutar c�lculos deterministas (comparar orden de etapas, combinar tipo
//  de proyecto con flags de obligatoriedad, concatenar rutas). Todo eso lo
//  precalcula ResolutorContexto y lo entrega ya resuelto en este DTO.
//
//  Coherente con el principio del proyecto: el plumbing del workflow es
//  c�digo determinista, no razonamiento de LLM.
//
//  Los enums ExigibilidadArtefacto y ObligatoriedadArtefacto se definen ac�,
//  una sola vez, en el namespace de contratos. Son enums PROPIOS del workflow
//  (no se comparten con las entidades EF); el contrato 3 (ArtefactoExtraido)
//  los referencia desde ac� sin redefinirlos.
// ============================================================================

namespace ISOAuditAgent.API.Agents.Contracts;

// ----------------------------------------------------------------------------
//  DTO principal
// ----------------------------------------------------------------------------

/// <summary>
/// Contexto completo de una auditor�a, listo para que DocumentAnalysis trabaje
/// sin consultar la BD. Lo produce ResolutorContexto.
/// </summary>
public sealed record ContextoAuditoria(
    int AuditoriaId,
    int ProyectoId,
    int EtapaId,
    string ProcedimientoCodigo,
    string ProcedimientoNombre,
    IntegracionesProyecto Integraciones,
    IReadOnlyList<ArtefactoEsperadoContexto> ArtefactosEsperados
);

/// <summary>
/// IDs de las integraciones externas del proyecto. Cada uno es nullable: un
/// proyecto puede no usar todas las herramientas (modelo_bd.md, tabla
/// Proyecto). DocumentAnalysis usa estos IDs para resolver a qu� fuente MCP
/// ir a buscar cada artefacto.
/// </summary>
public sealed record IntegracionesProyecto(
    string? DriveFolderId,
    string? TrelloBoardId,
    string? ClockifyProjectId,
    string? TemplatesFolderId  // Drive folder ID donde viven los templates (configuraciones_sistema)
);

/// <summary>
/// Un artefacto que el procedimiento espera, con todos los c�lculos
/// deterministas YA resueltos por ResolutorContexto.
/// </summary>
public sealed record ArtefactoEsperadoContexto(
    int ArtefactoEsperadoId,
    string? CodigoArtefacto,         // "FR 30", "FR 11"... null si no es FR formal
    string NombreArtefacto,          // "ERS", "Cronograma", "Casos de Prueba"
    int EtapaArtefactoId,            // etapa a la que pertenece este artefacto

    // --- C�lculos precalculados por ResolutorContexto ---

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
    string? NombreTemplateArchivo,  // nombre del archivo de template en la carpeta de templates
    ISOAuditAgent.API.Models.FuenteVerificacion FuenteVerificacion  // fuente donde se verifica la evidencia
);

// ----------------------------------------------------------------------------
//  Enums propios del workflow � definidos una sola vez, ac�
// ----------------------------------------------------------------------------

/// <summary>
/// Indica si un artefacto declarado en el procedimiento es exigible en la
/// auditor�a actual o si pertenece a una etapa futura del proyecto.
/// </summary>
public enum ExigibilidadArtefacto
{
    Exigible,
    PendienteEtapaFutura
}

/// <summary>
/// Obligatoriedad del artefacto, resuelta combinando el tipo de proyecto
/// (A/B) con los flags mandatorio_tipo_a / mandatorio_tipo_b del
/// ArtefactoEsperado.
/// </summary>
public enum ObligatoriedadArtefacto
{
    Mandatorio,
    EvaluarYJustificar
}