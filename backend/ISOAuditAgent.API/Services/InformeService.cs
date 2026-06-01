using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Servicio de gestion de informes.
/// Actualizado para usar los enums TipoInforme y EstadoAuditoria.
/// </summary>
public class InformeService
{
    private readonly IInformeRepository _informeRepo;
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IProyectoRepository _proyectoRepo;
    private readonly ILogger<InformeService> _logger;

    public InformeService(
        IInformeRepository informeRepo,
        IAuditoriaRepository auditoriaRepo,
        IProyectoRepository proyectoRepo,
        ILogger<InformeService> logger)
    {
        _informeRepo   = informeRepo;
        _auditoriaRepo = auditoriaRepo;
        _proyectoRepo  = proyectoRepo;
        _logger        = logger;
    }

    public async Task<IReadOnlyList<InformeResponse>> ObtenerTodosAsync()
    {
        var informes = await _informeRepo.ObtenerTodosAsync();
        return await MapearConDetallesAsync(informes);
    }

    public async Task<IReadOnlyList<InformeResponse>> ObtenerPorAuditoriaAsync(int auditoriaId)
    {
        var informes = await _informeRepo.ObtenerPorAuditoriaAsync(auditoriaId);
        return await MapearConDetallesAsync(informes);
    }

    public async Task<InformeResponse?> ObtenerPorIdAsync(int id)
    {
        var informe = await _informeRepo.ObtenerPorIdAsync(id);
        if (informe == null) return null;
        return await MapearConDetallesAsync(informe);
    }

    public async Task<InformeResponse> GenerarAutomaticoAsync(int auditoriaId)
    {
        var auditoria = await _auditoriaRepo.ObtenerPorIdAsync(auditoriaId)
            ?? throw new InvalidOperationException($"Auditoría con ID {auditoriaId} no encontrada");

        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(auditoria.ProyectoId);
        var contenido = GenerarContenido(auditoria, proyecto?.Nombre ?? "Proyecto no encontrado");

        var informe = new Informe
        {
            AuditoriaId     = auditoriaId,
            FechaGeneracion = DateTime.UtcNow,
            // Usamos el enum TipoInforme en lugar del string "automatico"
            Tipo            = TipoInforme.Auto,
            Contenido       = contenido,
            Activo          = true
        };

        var creado = await _informeRepo.CrearAsync(informe);
        _logger.LogInformation("Informe automatico generado | Id={Id}", creado.Id);
        return await MapearConDetallesAsync(creado);
    }

    public async Task<InformeResponse> GenerarManualAsync(GenerarInformeRequest request)
    {
        var auditoria = await _auditoriaRepo.ObtenerPorIdAsync(request.AuditoriaId)
            ?? throw new InvalidOperationException($"Auditoría con ID {request.AuditoriaId} no encontrada");

        // Solo se puede generar informe de auditorias completadas
        if (auditoria.Estado != EstadoAuditoria.Completada)
            throw new InvalidOperationException("Solo se puede generar informe de auditorias completadas");

        var proyecto  = await _proyectoRepo.ObtenerPorIdAsync(auditoria.ProyectoId);
        var contenido = GenerarContenido(auditoria, proyecto?.Nombre ?? "Proyecto no encontrado");

        var informe = new Informe
        {
            AuditoriaId     = request.AuditoriaId,
            FechaGeneracion = DateTime.UtcNow,
            Tipo            = TipoInforme.Manual,
            Contenido       = contenido,
            Activo = true
        };

        var creado = await _informeRepo.CrearAsync(informe);
        _logger.LogInformation("Informe manual generado | Id={Id}", creado.Id);
        return await MapearConDetallesAsync(creado);
    }

    private static string GenerarContenido(Auditoria auditoria, string nombreProyecto)
    {
        return
            "INFORME DE AUDITORIA ISO 9001\n" +
            "=============================\n" +
            $"Proyecto       : {nombreProyecto}\n" +
            $"Auditoria ID   : {auditoria.Id}\n" +
            $"Etapa          : {auditoria.EtapaId}\n" +
            $"Fecha inicio   : {auditoria.FechaInicioUtc:yyyy-MM-dd HH:mm} UTC\n" +
            $"Fecha fin      : {auditoria.FechaFinalizacionUtc?.ToString("yyyy-MM-dd HH:mm") ?? "En curso"} UTC\n" +
            $"Estado         : {auditoria.Estado}";
    }

    private async Task<IReadOnlyList<InformeResponse>> MapearConDetallesAsync(
        IReadOnlyList<Informe> informes)
    {
        var resultado = new List<InformeResponse>();
        foreach (var i in informes)
            resultado.Add(await MapearConDetallesAsync(i));
        return resultado;
    }

    private async Task<InformeResponse> MapearConDetallesAsync(Informe i)
    {
        var auditoria = await _auditoriaRepo.ObtenerPorIdAsync(i.AuditoriaId);
        var proyecto  = auditoria != null
            ? await _proyectoRepo.ObtenerPorIdAsync(auditoria.ProyectoId)
            : null;

        return new InformeResponse(
            i.Id,
            i.AuditoriaId,
            proyecto?.Nombre ?? "Proyecto no encontrado",
            i.FechaGeneracion,
            // El tipo ahora es un enum — lo convertimos a string para el DTO
            i.Tipo.ToString(),
            i.Contenido);
    }
}