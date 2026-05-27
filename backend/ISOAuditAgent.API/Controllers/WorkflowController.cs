// using ISOAuditAgent.API.DTOs;
// using ISOAuditAgent.API.Services;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;

// namespace ISOAuditAgent.API.Controllers;

// /// <summary>
// /// Controller del workflow de auditoria.
// ///
// /// Es el punto de entrada del orquestador al sistema.
// /// El auditor llama a este endpoint para iniciar una auditoria.
// ///
// /// Endpoints:
// ///   POST /api/workflow/iniciar → inicia el workflow completo
// /// </summary>
// [ApiController]
// [Route("api/workflow")]
// [Authorize]
// public class WorkflowController : ControllerBase
// {
//     private readonly ResolutorContextoService _resolutorContexto;
//     private readonly ILogger<WorkflowController> _logger;

//     public WorkflowController(
//         ResolutorContextoService resolutorContexto,
//         ILogger<WorkflowController> logger)
//     {
//         _resolutorContexto = resolutorContexto;
//         _logger            = logger;
//     }

//     /// <summary>
//     /// Inicia el workflow de auditoria.
//     ///
//     /// Paso 1: El ResolutorContexto lee la BD y arma el ContextoAuditoria.
//     /// Paso 2: El ContextoAuditoria se devuelve al orquestador.
//     /// Paso 3: El orquestador lo distribuye a los agentes (DocumentAnalysis,
//     ///         ComplianceValidation, ConsistencyVerification, etc.)
//     ///
//     /// El ID del auditor se toma del token JWT — el usuario no lo manda.
//     /// </summary>
//     [HttpPost("iniciar")]
//     public async Task<IActionResult> IniciarWorkflow(
//         [FromBody] IniciarAuditoriaRequest request,
//         CancellationToken ct)
//     {
//         if (!ModelState.IsValid)
//             return BadRequest(ModelState);

//         // El ID del auditor viene del token JWT
//         var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
//         if (idClaim == null || !int.TryParse(idClaim, out var usuarioId))
//             return Unauthorized();

//         _logger.LogInformation(
//             "Workflow iniciado | ProyectoId={PId} | EtapaId={EId} | UsuarioId={UId}",
//             request.ProyectoId, request.EtapaId, usuarioId);

//         try
//         {
//             // Nodo 1 — ResolutorContexto
//             // Lee la BD y arma el contexto completo para los agentes
//             var contexto = await _resolutorContexto.ResolverAsync(request, usuarioId, ct);

//             _logger.LogInformation(
//                 "ContextoAuditoria armado | AuditoriaId={AId} | Artefactos={AC} | Reglas={RC}",
//                 contexto.AuditoriaId,
//                 contexto.ArtefactosEsperados.Count,
//                 contexto.ReglasValidacion.Count);

//             // Devolvemos el contexto al orquestador
//             // El orquestador lo distribuye a los agentes en los nodos siguientes
//             return Ok(contexto);
//         }
//         catch (InvalidOperationException ex)
//         {
//             return NotFound(new { mensaje = ex.Message });
//         }
//     }
// }