using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Servicio de gestion de auditorias.
/// Actualizado para usar los enums de los modelos de Matias:
/// EstadoAuditoria en lugar de strings "en_curso", "completada", etc.
/// </summary>
public class AuditoriaService
{
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IProyectoRepository _proyectoRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ILogger<AuditoriaService> _logger;

    public AuditoriaService(
        IAuditoriaRepository auditoriaRepo,
        IProyectoRepository proyectoRepo,
        IUsuarioRepository usuarioRepo,
        ILogger<AuditoriaService> logger)
    {
        _auditoriaRepo = auditoriaRepo;
        _proyectoRepo  = proyectoRepo;
        _usuarioRepo   = usuarioRepo;
        _logger        = logger;
    }

    public async Task<IReadOnlyList<AuditoriaResponse>> ObtenerTodasAsync()
    {
        var auditorias = await _auditoriaRepo.ObtenerTodasAsync();
        return await MapearConDetallesAsync(auditorias);
    }

    public async Task<IReadOnlyList<AuditoriaResponse>> ObtenerPorProyectoAsync(int proyectoId)
    {
        var auditorias = await _auditoriaRepo.ObtenerPorProyectoAsync(proyectoId);
        return await MapearConDetallesAsync(auditorias);
    }

    public async Task<AuditoriaResponse?> ObtenerPorIdAsync(int id)
    {
        var auditoria = await _auditoriaRepo.ObtenerPorIdAsync(id);
        if (auditoria == null) return null;
        return await MapearConDetallesAsync(auditoria);
    }

    public async Task<AuditoriaResponse> CrearAsync(CrearAuditoriaRequest request, int usuarioId)
    {
        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(request.ProyectoId)
            ?? throw new InvalidOperationException($"Proyecto con ID {request.ProyectoId} no encontrado");

        var auditoria = new Auditoria
        {
            ProyectoId     = request.ProyectoId,
            UsuarioId      = usuarioId,
            EtapaId        = request.EtapaId,
            FechaInicioUtc = DateTime.UtcNow,
            // Usamos el enum EstadoAuditoria en lugar del string "en_curso"
            Estado         = EstadoAuditoria.EnCurso,
            Activo = true,
        };

        var creada = await _auditoriaRepo.CrearAsync(auditoria);

        _logger.LogInformation(
            "Auditoria creada | Id={Id} | ProyectoId={PId} | UsuarioId={UId}",
            creada.Id, creada.ProyectoId, creada.UsuarioId);

        return await MapearConDetallesAsync(creada);
    }

    public async Task<AuditoriaResponse?> ActualizarEstadoAsync(int id, EstadoAuditoria estado)
    {
        var auditoria = await _auditoriaRepo.ObtenerPorIdAsync(id);
        if (auditoria == null) return null;

        auditoria.Estado               = estado;
        auditoria.FechaFinalizacionUtc = DateTime.UtcNow;

        var actualizada = await _auditoriaRepo.ActualizarAsync(auditoria);

        _logger.LogInformation(
            "Auditoria actualizada | Id={Id} | Estado={Estado}",
            actualizada.Id, actualizada.Estado);

        return await MapearConDetallesAsync(actualizada);
    }

    private async Task<IReadOnlyList<AuditoriaResponse>> MapearConDetallesAsync(
        IReadOnlyList<Auditoria> auditorias)
    {
        var resultado = new List<AuditoriaResponse>();
        foreach (var a in auditorias)
            resultado.Add(await MapearConDetallesAsync(a));
        return resultado;
    }

    private async Task<AuditoriaResponse> MapearConDetallesAsync(Auditoria a)
    {
        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(a.ProyectoId);
        var usuario  = await _usuarioRepo.ObtenerPorIdAsync(a.UsuarioId);

        return new AuditoriaResponse(
            a.Id,
            a.ProyectoId,
            proyecto?.Nombre ?? "Proyecto no encontrado",
            a.UsuarioId,
            usuario?.Nombre ?? "Usuario no encontrado",
            a.EtapaId,
            a.FechaInicioUtc,
            a.FechaFinalizacionUtc,
            // El estado ahora es un enum — lo convertimos a string para el DTO
            a.Estado.ToString());
    }
}