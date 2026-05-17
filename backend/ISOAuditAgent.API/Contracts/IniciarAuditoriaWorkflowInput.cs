namespace ISOAuditAgent.Contracts;

/// <summary>
/// Contrato 1 — Input del workflow del orquestador.
/// Producido por la API REST (POST /api/auditorias) al disparar el
/// workflow, después de crear la auditoría con estado <c>en_curso</c>.
/// Consumido por el primer nodo del grafo (DocumentAnalysis).
/// </summary>
/// <remarks>
/// <para>
/// Definición alineada con <c>contratos_agentes.md §3.1</c> (v2.1).
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>AuditoriaId</c>: auditoría ya creada por la API REST.
///   </description></item>
///   <item><description>
///     <c>ProyectoId</c>: el workflow lo usa para resolver
///     <c>drive_folder_id</c>, <c>trello_board_id</c>,
///     <c>clockify_project_id</c> y <c>tipo_proyecto</c> (A o B) por
///     consulta a BD.
///   </description></item>
///   <item><description>
///     <c>EtapaId</c>: etapa actual del proyecto, indicada por el usuario.
///     Determina la exigibilidad de cada artefacto (etapas anteriores y
///     actual = exigibles; etapas futuras = <c>PendienteEtapaFutura</c>).
///   </description></item>
/// </list>
/// <para>
/// Información derivable (<c>UsuarioEjecutorId</c>, <c>FechaInicioUtc</c>,
/// <c>ProcedimientoId</c>) se consulta desde BD por <c>AuditoriaId</c> /
/// <c>ProyectoId</c>; no se duplica en el DTO.
/// </para>
/// </remarks>
public sealed record IniciarAuditoriaWorkflowInput(
    int AuditoriaId,
    int ProyectoId,
    int EtapaId
);
