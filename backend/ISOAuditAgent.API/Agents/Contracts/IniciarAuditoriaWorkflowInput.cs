
// ============================================================================
//  Contrato 1 — Input del workflow de auditoría (contratos_agentes.md v2.2)
// ----------------------------------------------------------------------------
//  Lo produce la API REST al disparar el workflow, después de crear la
//  auditoría con estado EnCurso. Lo consume el primer nodo del grafo,
//  ResolutorContexto.
//
//  Notas (contratos_agentes.md, contrato 1):
//   - AuditoriaId: auditoría ya creada por la API.
//   - ProyectoId: ResolutorContexto lo usa para resolver drive_folder_id,
//     trello_board_id, clockify_project_id, tipo_proyecto (A/B) y
//     procedimiento_id por consulta a BD.
//   - EtapaId: etapa actual del proyecto, indicada por el usuario.
//     Determina la exigibilidad de cada artefacto.
//   - Información derivable (UsuarioEjecutorId, FechaInicioUtc) se consulta
//     desde BD vía AuditoriaId. No se duplica en el DTO.
// ============================================================================

namespace ISOAuditAgent.API.Agents.Contracts;

/// <summary>
/// Input del workflow del orquestador. Lo produce la API REST tras crear la
/// auditoría en estado EnCurso; lo consume el nodo ResolutorContexto.
/// </summary>
public sealed record IniciarAuditoriaWorkflowInput(
    int AuditoriaId,
    int ProyectoId,
    int EtapaId
);