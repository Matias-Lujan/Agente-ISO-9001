using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Servicio de gestion de reglas de validacion.
///
/// Las reglas NO las ingresa el auditor — las define el procedimiento PR 11-13.
/// El Administrador puede gestionarlas si el procedimiento cambia.
/// El orquestador las lee para saber que validar en cada etapa.
/// </summary>
public class ReglaValidacionService
{
    private readonly IReglaValidacionRepository _repo;
    private readonly ILogger<ReglaValidacionService> _logger;

    public ReglaValidacionService(
        IReglaValidacionRepository repo,
        ILogger<ReglaValidacionService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    // ── CONSULTAS — las usa el orquestador ───────────────────────────────────

    /// <summary>
    /// Devuelve todas las reglas activas de un procedimiento.
    /// El orquestador llama a esto para saber que validar.
    /// </summary>
    public async Task<IReadOnlyList<ReglaValidacionResponse>> ObtenerPorProcedimientoAsync(int procedimientoId)
    {
        var reglas = await _repo.ObtenerPorProcedimientoAsync(procedimientoId);
        return reglas.Select(MapearRegla).ToList();
    }

    /// <summary>
    /// Devuelve las reglas activas de una etapa especifica.
    /// El orquestador llama a esto al auditar una etapa particular.
    /// </summary>
    public async Task<IReadOnlyList<ReglaValidacionResponse>> ObtenerPorEtapaAsync(int etapaId)
    {
        var reglas = await _repo.ObtenerPorEtapaAsync(etapaId);
        return reglas.Select(MapearRegla).ToList();
    }

    // ── GESTION — solo el Administrador ──────────────────────────────────────

    /// <summary>
    /// Crea una regla nueva.
    /// Solo el Administrador puede hacer esto — si el procedimiento cambia.
    /// </summary>
    public async Task<ReglaValidacionResponse> CrearAsync(CrearReglaRequest request)
    {
        // Validar que el tipo de regla sea valido
        var tiposValidos = new[]
        {
            TiposRegla.Existencia,
            TiposRegla.Contenido,
            TiposRegla.Consistencia,
            TiposRegla.Secuencia,
            TiposRegla.Responsable
        };

        if (!tiposValidos.Contains(request.Tipo))
            throw new ArgumentException($"Tipo de regla invalido: {request.Tipo}");

        var regla = new ReglaValidacion
        {
            ProcedimientoId = request.ProcedimientoId,
            EtapaId         = request.EtapaId,
            Codigo          = request.Codigo,
            Descripcion     = request.Descripcion,
            Tipo            = request.Tipo,
            Obligatoria     = request.Obligatoria,
            Activa          = true
        };

        var creada = await _repo.CrearAsync(regla);

        _logger.LogInformation(
            "Regla creada | Id={Id} | Codigo={Codigo} | Tipo={Tipo}",
            creada.Id, creada.Codigo, creada.Tipo);

        return MapearRegla(creada);
    }

    /// <summary>
    /// Modifica una regla existente.
    /// Solo el Administrador puede hacer esto.
    /// </summary>
    public async Task<ReglaValidacionResponse?> ModificarAsync(int id, ModificarReglaRequest request)
    {
        var regla = await _repo.ObtenerPorIdAsync(id);
        if (regla == null) return null;

        if (request.Codigo      != null) regla.Codigo      = request.Codigo;
        if (request.Descripcion != null) regla.Descripcion = request.Descripcion;
        if (request.Tipo        != null) regla.Tipo        = request.Tipo;
        if (request.Obligatoria != null) regla.Obligatoria = request.Obligatoria.Value;
        if (request.Activa      != null) regla.Activa      = request.Activa.Value;

        var actualizada = await _repo.ActualizarAsync(regla);
        return MapearRegla(actualizada);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static ReglaValidacionResponse MapearRegla(ReglaValidacion r) =>
        new(r.Id, r.ProcedimientoId, r.EtapaId, r.Codigo,
            r.Descripcion, r.Tipo, r.Obligatoria, r.Activa);
}