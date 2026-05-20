using ISOAuditAgent.API.Agents.ConsistencyVerification;
using ISOAuditAgent.API.Agents.ConsistencyVerification.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Endpoint REST del agente de Verificacion de Consistencia.
///
/// En el sistema completo, este endpoint lo llama el orquestador.
/// Para la demo, lo llamamos directamente desde la terminal con demo.json.
///
/// ENTRADA:  DocumentosExtraidos (Contrato 2) — viene de DocumentAnalysis
/// SALIDA:   HallazgosPreliminares (Contrato 3) — va a FindingsClassification
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
    /// Ejecuta el analisis de consistencia sobre los artefactos recibidos.
    /// Devuelve los hallazgos preliminares para que FindingsClassification los clasifique.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(HallazgosPreliminares), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Execute(
        [FromBody] DocumentosExtraidos input,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (input.Artefactos == null || input.Artefactos.Count == 0)
            return BadRequest("Se requiere al menos un artefacto para analizar.");

        _logger.LogInformation(
            "POST /api/consistency-verification | AuditoriaId={Id} | Artefactos={Count}",
            input.AuditoriaId, input.Artefactos.Count);

        var resultado = await _agent.ExecuteAsync(input, ct);

        return Ok(resultado);
    }

    /// <summary>
    /// Health check — el orquestador lo usa para verificar que el agente esta activo.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health() =>
        Ok(new { agent = "ConsistencyVerification", status = "ok" });
}
