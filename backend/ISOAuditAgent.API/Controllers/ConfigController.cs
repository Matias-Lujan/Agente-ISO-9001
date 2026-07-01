using System.Security.Claims;
using ISOAuditAgent.API.Agents.DocumentAnalysis;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;
using ISOAuditAgent.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISOAuditAgent.API.Controllers;

/// <summary>
/// Configuracion del sistema.
///
///  - GET /api/config                       → valores informativos (modelo IA).
///    Cualquier usuario autenticado. NUNCA devuelve secretos.
///  - /api/config/prompts/...               → gestion de los system prompts de
///    los agentes. SOLO Administrador: editar mal un prompt puede romper el
///    pipeline de auditoria, por eso queda restringido y con historial/rollback.
/// </summary>
[ApiController]
[Route("api/config")]
[Authorize]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IPromptStore _prompts;
    private readonly IConsumoTokensRepository _consumoTokens;

    public ConfigController(
        IConfiguration config,
        IPromptStore prompts,
        IConsumoTokensRepository consumoTokens)
    {
        _config = config;
        _prompts = prompts;
        _consumoTokens = consumoTokens;
    }

    /// <summary>
    /// Configuracion visible del sistema (de momento, el modelo de IA).
    /// Cualquier usuario autenticado.
    /// </summary>
    [HttpGet]
    public IActionResult Obtener()
    {
        return Ok(new
        {
            modeloIa = _config["Gemini:ModelId"] ?? "No configurado",
        });
    }

    // ── CONSUMO DE TOKENS DEL LLM (solo Administrador) ────────────────────────

    /// <summary>
    /// KPI de consumo de tokens de la app: total (entrada/salida/total, cantidad
    /// de llamadas y de auditorías) + desglose por agente. Solo Administrador:
    /// es información de costo/uso del sistema.
    /// </summary>
    [HttpGet("consumo-tokens")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ObtenerConsumoTokens(CancellationToken ct)
    {
        var resumen = await _consumoTokens.ObtenerResumenAsync(ct);
        return Ok(resumen);
    }

    // ── SYSTEM PROMPTS DE LOS AGENTES (solo Administrador) ────────────────────

    /// <summary>
    /// Devuelve el prompt activo de un agente + su historial de versiones.
    /// </summary>
    [HttpGet("prompts/{agenteKey}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ObtenerPrompt(string agenteKey, CancellationToken ct)
    {
        if (!SystemPrompts.Defaults.ContainsKey(agenteKey))
            return NotFound(new { mensaje = $"Agente desconocido: '{agenteKey}'." });

        var activo = await _prompts.ObtenerActivoAsync(agenteKey, ct);
        var historial = await _prompts.ObtenerHistorialAsync(agenteKey, ct);

        return Ok(MapearRespuesta(agenteKey, activo, historial));
    }

    /// <summary>
    /// Guarda una versión nueva del prompt (queda activa).
    /// </summary>
    [HttpPut("prompts/{agenteKey}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> ActualizarPrompt(
        string agenteKey, [FromBody] ActualizarPromptRequest request, CancellationToken ct)
    {
        return await EjecutarYDevolver(agenteKey, ct, () =>
            _prompts.GuardarNuevaVersionAsync(agenteKey, request.Contenido, UsuarioId(), request.Comentario, ct));
    }

    /// <summary>
    /// Restablece el prompt al valor por defecto del sistema (nueva versión).
    /// </summary>
    [HttpPost("prompts/{agenteKey}/reset")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> RestablecerPrompt(string agenteKey, CancellationToken ct)
    {
        return await EjecutarYDevolver(agenteKey, ct, () =>
            _prompts.RestablecerDefaultAsync(agenteKey, UsuarioId(), ct));
    }

    /// <summary>
    /// Revierte a una versión anterior (crea una nueva versión con ese contenido).
    /// </summary>
    [HttpPost("prompts/{agenteKey}/revert/{version:int}")]
    [Authorize(Roles = Roles.Administrador)]
    public async Task<IActionResult> RevertirPrompt(string agenteKey, int version, CancellationToken ct)
    {
        return await EjecutarYDevolver(agenteKey, ct, () =>
            _prompts.RevertirAsync(agenteKey, version, UsuarioId(), ct));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<IActionResult> EjecutarYDevolver(
        string agenteKey, CancellationToken ct, Func<Task<PromptAgente>> accion)
    {
        try
        {
            await accion();
            var activo = await _prompts.ObtenerActivoAsync(agenteKey, ct);
            var historial = await _prompts.ObtenerHistorialAsync(agenteKey, ct);
            return Ok(MapearRespuesta(agenteKey, activo, historial));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    private int? UsuarioId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out var id) ? id : null;
    }

    private static PromptAgenteResponse MapearRespuesta(
        string agenteKey, string contenidoActivo, IReadOnlyList<PromptAgente> historial)
    {
        var esDefault = string.Equals(
            contenidoActivo, SystemPrompts.Defaults[agenteKey], StringComparison.Ordinal);

        var versionActiva = historial.FirstOrDefault(h => h.EsActiva)?.Version ?? 0;

        var versiones = historial
            .Select(h => new PromptVersionResponse(
                h.Version,
                h.EsActiva,
                h.ModificadoPorUsuarioId,
                h.ModificadoPor?.Nombre,
                h.FechaCreacion,
                h.Comentario))
            .ToList();

        return new PromptAgenteResponse(agenteKey, contenidoActivo, esDefault, versionActiva, versiones);
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record ActualizarPromptRequest(string Contenido, string? Comentario);

public record PromptVersionResponse(
    int Version,
    bool EsActiva,
    int? ModificadoPorUsuarioId,
    string? ModificadoPorNombre,
    DateTime FechaCreacion,
    string? Comentario);

public record PromptAgenteResponse(
    string AgenteKey,
    string Contenido,
    bool EsDefault,
    int VersionActiva,
    IReadOnlyList<PromptVersionResponse> Historial);
