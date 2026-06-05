using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Servicio de gestion de procedimientos, etapas y artefactos esperados.
///
/// Se encarga de:
/// 1. Consultar procedimientos y sus etapas
/// 2. Consultar artefactos esperados por etapa
/// 3. Crear y modificar artefactos esperados (solo Administrador)
///
/// No sabe nada de base de datos — le delega eso al IProcedimientoRepository.
/// No sabe nada de HTTP — eso lo maneja el Controller.
/// </summary>
public class ProcedimientoService
{
    private readonly IProcedimientoRepository _repo;
    private readonly ILogger<ProcedimientoService> _logger;

    public ProcedimientoService(
        IProcedimientoRepository repo,
        ILogger<ProcedimientoService> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    // ── PROCEDIMIENTOS ────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve todos los procedimientos.
    /// </summary>
    public async Task<IReadOnlyList<ProcedimientoResponse>> ObtenerTodosAsync()
    {
        var procedimientos = await _repo.ObtenerTodosAsync();
        return procedimientos.Select(MapearProcedimiento).ToList();
    }

    /// <summary>
    /// Devuelve un procedimiento con todas sus etapas y artefactos.
    /// </summary>
    public async Task<ProcedimientoResponse?> ObtenerPorIdAsync(int id)
    {
        var procedimiento = await _repo.ObtenerPorIdAsync(id);
        if (procedimiento == null) return null;

        return MapearProcedimiento(procedimiento);
    }

    // ── ETAPAS ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve las etapas de un procedimiento con sus artefactos.
    /// </summary>
    public async Task<IReadOnlyList<EtapaResponse>> ObtenerEtapasAsync(int procedimientoId)
    {
        var etapas = await _repo.ObtenerEtapasPorProcedimientoAsync(procedimientoId);
        var resultado = new List<EtapaResponse>();

        foreach (var etapa in etapas)
        {
            var artefactos = await _repo.ObtenerArtefactosPorEtapaAsync(etapa.Id);
            resultado.Add(MapearEtapa(etapa, artefactos));
        }

        return resultado;
    }

    // ── ARTEFACTOS ESPERADOS ──────────────────────────────────────────────────

    /// <summary>
    /// Devuelve los artefactos esperados de una etapa.
    /// </summary>
    public async Task<IReadOnlyList<ArtefactoEsperadoResponse>> ObtenerArtefactosAsync(int etapaId)
    {
        var artefactos = await _repo.ObtenerArtefactosPorEtapaAsync(etapaId);
        return artefactos.Select(MapearArtefacto).ToList();
    }

    /// <summary>
    /// Crea un artefacto esperado nuevo.
    /// Solo el Administrador puede crear artefactos.
    /// </summary>
    public async Task<ArtefactoEsperadoResponse> CrearArtefactoAsync(CrearArtefactoRequest request)
    {
        var artefacto = new ArtefactoEsperado
        {
            EtapaId              = request.EtapaId,
            Codigo               = request.Codigo,
            Nombre               = request.Nombre,
            Descripcion          = request.Descripcion,
            MandatorioTipoA      = request.MandatorioTipoA,
            MandatorioTipoB      = request.MandatorioTipoB,
            PathTemplateRelativo = request.PathTemplateRelativo
        };

        var creado = await _repo.CrearArtefactoAsync(artefacto);

        _logger.LogInformation(
            "Artefacto creado | Id={Id} | Nombre={Nombre} | EtapaId={EtapaId}",
            creado.Id, creado.Nombre, creado.EtapaId);

        return MapearArtefacto(creado);
    }

    /// <summary>
    /// Modifica un artefacto esperado existente.
    /// Solo actualiza los campos que vienen con valor.
    /// </summary>
    public async Task<ArtefactoEsperadoResponse?> ModificarArtefactoAsync(
        int id, ModificarArtefactoRequest request)
    {
        var artefacto = await _repo.ObtenerArtefactoPorIdAsync(id);
        if (artefacto == null) return null;

        if (request.Codigo               != null) artefacto.Codigo               = request.Codigo;
        if (request.Nombre               != null) artefacto.Nombre               = request.Nombre;
        if (request.Descripcion          != null) artefacto.Descripcion          = request.Descripcion;
        if (request.MandatorioTipoA      != null) artefacto.MandatorioTipoA      = request.MandatorioTipoA.Value;
        if (request.MandatorioTipoB      != null) artefacto.MandatorioTipoB      = request.MandatorioTipoB.Value;
        if (request.PathTemplateRelativo != null) artefacto.PathTemplateRelativo = request.PathTemplateRelativo;

        var actualizado = await _repo.ActualizarArtefactoAsync(artefacto);
        return MapearArtefacto(actualizado);
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    private static ProcedimientoResponse MapearProcedimiento(Procedimiento p) =>
        new(p.Id, p.Codigo, p.Nombre, p.Descripcion);

    private static EtapaResponse MapearEtapa(Etapa e, IReadOnlyList<ArtefactoEsperado> artefactos) =>
        new(e.Id, e.ProcedimientoId, e.Nombre, e.Orden, e.Descripcion,
            artefactos.Select(MapearArtefacto).ToList());

    private static ArtefactoEsperadoResponse MapearArtefacto(ArtefactoEsperado a) =>
        new(a.Id, a.EtapaId, a.Codigo, a.Nombre, a.Descripcion,
            a.MandatorioTipoA, a.MandatorioTipoB, a.PathTemplateRelativo);
}