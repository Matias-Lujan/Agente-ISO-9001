using ISOAuditAgent.API.Agents.FindingsClassification.Contracts;
using ISOAuditAgent.API.Agents.FindingsClassification.Executors;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Endpoint REST del agente de Clasificación de Hallazgos.
///
/// En el sistema completo, este endpoint lo llama el orquestador.
/// Recibe la lista de HallazgosPreliminares producida por ComplianceValidation
/// y ConsistencyVerification y devuelve los HallazgosClasificados (NC/OBS/OM).
///
/// ENTRADA: List&lt;HallazgosPreliminares&gt; — un grupo por agente validador
/// SALIDA:  HallazgosClasificados — listo para el ConsolidadorResultado
/// </summary>
[ApiController]
[Route("api/findings-classification")]
public class FindingsClassificationController : ControllerBase
{
    private readonly FindingsClassificationExecutor _executor;
    private readonly ILogger<FindingsClassificationController> _logger;

    public FindingsClassificationController(
        FindingsClassificationExecutor executor,
        ILogger<FindingsClassificationController> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    /// <summary>
    /// Clasifica los hallazgos preliminares recibidos.
    /// Esperás recibir los dos lotes (Compliance + Consistency) en el body.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(HallazgosClasificados), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Execute(
        [FromBody] List<HallazgosPreliminares> grupos,
        CancellationToken ct)
    {
        if (grupos == null || grupos.Count == 0)
            return BadRequest("Se requiere al menos un grupo de hallazgos preliminares.");

        var totalHallazgos = grupos.Sum(g => g.Hallazgos?.Count ?? 0);
        if (totalHallazgos == 0)
            return BadRequest("Los grupos recibidos no contienen hallazgos para clasificar.");

        _logger.LogInformation(
            "POST /api/findings-classification | AuditoriaId={Id} | Grupos={Grupos} | Total={Total}",
            grupos[0].AuditoriaId, grupos.Count, totalHallazgos);

        var resultado = await _executor.ClasificarAsync(grupos, ct);
        return Ok(resultado);
    }

    /// <summary>
    /// Health check — el orquestador lo usa para verificar que el agente está activo.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health() =>
        Ok(new { agent = "FindingsClassification", status = "ok" });
}
