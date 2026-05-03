using ISOAuditAgent.API.Agents.ConsistencyVerification;
using ISOAuditAgent.API.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Endpoint REST que el orquestador invoca para ejecutar el agente 4.4.
/// POST /api/consistency-verification
/// </summary>
[ApiController]
[Route("api/consistency-verification")]
public class ConsistencyVerificationController : ControllerBase
{
    private readonly ConsistencyVerificationAgentService _agent;
    private readonly ILogger<ConsistencyVerificationController> _logger;

    public ConsistencyVerificationController(
        ConsistencyVerificationAgentService agent,
        ILogger<ConsistencyVerificationController> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el análisis de consistencia sobre un conjunto de documentos.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ConsistencyVerificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Execute(
        [FromBody] ConsistencyVerificationRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.Documents is null || request.Documents.Count == 0)
            return BadRequest("Se requiere al menos un documento para analizar.");

        _logger.LogInformation(
            "POST /api/consistency-verification | AuditId={Id}",
            request.AuditId);

        var result = await _agent.ExecuteAsync(request, ct);

        return result.Status == AgentStatus.Failed
            ? StatusCode(500, result)
            : Ok(result);
    }

    /// <summary>
    /// Health check para que el orquestador verifique que el agente está activo.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health() =>
        Ok(new { agent = "ConsistencyVerification", status = "ok" });
}
