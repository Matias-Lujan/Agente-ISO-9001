using ISOAuditAgent.API.Agents.ComplianceValidation;
using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Controlador que expone el Agente de Validación de Procesos ISO 9001 como endpoint HTTP.
/// Recibe solicitudes de validación y retorna hallazgos de inconsistencias.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ComplianceValidationController : ControllerBase
{
    private readonly IComplianceValidationAgent _agent;
    private readonly ILogger<ComplianceValidationController> _logger;

    public ComplianceValidationController(
        IComplianceValidationAgent agent,
        ILogger<ComplianceValidationController> logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Valida un proceso específico cruzando datos de Trello y Clockify.
    /// Retorna una lista de inconsistencias detectadas.
    /// </summary>
    /// <param name="projectId">Identificador del proyecto (Trello/Clockify).</param>
    /// <param name="procesoId">Identificador del proceso ISO 9001.</param>
    /// <returns>Lista de hallazgos de validación.</returns>
    /// <response code="200">Validación completada exitosamente. Retorna lista de hallazgos (puede estar vacía).</response>
    /// <response code="400">Parámetros de entrada inválidos.</response>
    /// <response code="500">Error interno en el servidor.</response>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ComplianceValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ComplianceValidationResponse>> ValidateProcess(
        [FromQuery] string projectId,
        [FromQuery] int procesoId)
    {
        try
        {
            // Validar parámetros
            if (string.IsNullOrWhiteSpace(projectId))
            {
                _logger.LogWarning("Validación rechazada: projectId vacío");
                return BadRequest(new ErrorResponse
                {
                    Mensaje = "El parámetro 'projectId' es requerido y no puede estar vacío.",
                    Codigo = "PARAM_INVALID"
                });
            }

            if (procesoId <= 0)
            {
                _logger.LogWarning("Validación rechazada: procesoId inválido ({procesoId})", procesoId);
                return BadRequest(new ErrorResponse
                {
                    Mensaje = "El parámetro 'procesoId' debe ser un número mayor a 0.",
                    Codigo = "PARAM_INVALID"
                });
            }

            _logger.LogInformation("Iniciando validación: projectId={projectId}, procesoId={procesoId}", 
                projectId, procesoId);

            // Invocar el agente
            var hallazgos = await _agent.ValidateProcessAsync(projectId, procesoId);

            _logger.LogInformation("Validación completada: {hallazgosCount} hallazgos detectados", 
                hallazgos.Count);

            // Retornar respuesta
            var response = new ComplianceValidationResponse
            {
                ExitosoValidacion = true,
                HallazgosCount = hallazgos.Count,
                Hallazgos = hallazgos,
                FechaAnalisis = DateTime.UtcNow,
                ProjectId = projectId,
                ProcesoId = procesoId
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la validación de procesos");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Mensaje = "Error interno en el servidor durante la validación.",
                Codigo = "VALIDATION_ERROR",
                Detalles = ex.Message
            });
        }
    }

    /// <summary>
    /// Ejecuta la validación de cumplimiento usando el nuevo contrato MAF (Microsoft Agent Framework).
    /// Recibe documentos extraídos y retorna hallazgos preliminares estructurados.
    /// </summary>
    /// <param name="input">Datos de entrada con AuditoriaId, ProyectoId y documentos a analizar.</param>
    /// <returns>Hallazgos preliminares con evidencias y justificaciones.</returns>
    /// <response code="200">Validación MAF completada exitosamente. Retorna HallazgosPreliminares.</response>
    /// <response code="400">Parámetros de entrada inválidos.</response>
    /// <response code="500">Error interno en el servidor.</response>
    [HttpPost("validate-maf")]
    [ProducesResponseType(typeof(HallazgosPreliminares), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HallazgosPreliminares>> ValidateProcessMaf(
        [FromBody] DocumentosExtraidos input)
    {
        try
        {
            // Validar parámetros
            if (input == null)
            {
                _logger.LogWarning("Validación MAF rechazada: input es null");
                return BadRequest(new ErrorResponse
                {
                    Mensaje = "El cuerpo de la solicitud (input) es requerido.",
                    Codigo = "PARAM_INVALID"
                });
            }

            if (input.AuditoriaId <= 0)
            {
                _logger.LogWarning("Validación MAF rechazada: AuditoriaId inválido ({AuditoriaId})", input.AuditoriaId);
                return BadRequest(new ErrorResponse
                {
                    Mensaje = "El parámetro 'AuditoriaId' debe ser un número mayor a 0.",
                    Codigo = "PARAM_INVALID"
                });
            }

            if (input.ProyectoId <= 0)
            {
                _logger.LogWarning("Validación MAF rechazada: ProyectoId inválido ({ProyectoId})", input.ProyectoId);
                return BadRequest(new ErrorResponse
                {
                    Mensaje = "El parámetro 'ProyectoId' debe ser un número mayor a 0.",
                    Codigo = "PARAM_INVALID"
                });
            }

            _logger.LogInformation("Iniciando validación MAF: AuditoriaId={AuditoriaId}, ProyectoId={ProyectoId}, ArtefactosCount={ArtefactosCount}",
                input.AuditoriaId, input.ProyectoId, input.Artefactos?.Count ?? 0);

            // Invocar el agente con el nuevo contrato MAF
            var hallazgosPreliminares = await _agent.ExecuteAsync(input);

            _logger.LogInformation("Validación MAF completada: {HallazgosCount} hallazgos preliminares generados por agente {AgenteOrigen}",
                hallazgosPreliminares.Hallazgos.Count, hallazgosPreliminares.AgenteOrigen);

            return Ok(hallazgosPreliminares);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la validación MAF para auditoría {AuditoriaId}", input?.AuditoriaId.ToString() ?? "UNKNOWN");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Mensaje = "Error interno en el servidor durante la validación MAF.",
                Codigo = "VALIDATION_ERROR",
                Detalles = ex.Message
            });
        }
    }

    /// <summary>
    /// Health check para verificar que el agente está disponible.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

/// <summary>
/// Respuesta exitosa de validación.
/// </summary>
public class ComplianceValidationResponse
{
    /// <summary>
    /// Indica si la validación se completó exitosamente.
    /// </summary>
    public bool ExitosoValidacion { get; set; } = true;

    /// <summary>
    /// Cantidad de hallazgos detectados.
    /// </summary>
    public int HallazgosCount { get; set; }

    /// <summary>
    /// Lista de hallazgos detectados.
    /// </summary>
    public List<ValidationFinding> Hallazgos { get; set; } = [];

    /// <summary>
    /// Timestamp del análisis.
    /// </summary>
    public DateTime FechaAnalisis { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identificador del proyecto analizado.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del proceso analizado.
    /// </summary>
    public int ProcesoId { get; set; }
}

/// <summary>
/// Respuesta de error.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Mensaje de error legible para el usuario.
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Código de error para clasificación.
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Detalles técnicos adicionales (solo en desarrollo).
    /// </summary>
    public string? Detalles { get; set; }
}
