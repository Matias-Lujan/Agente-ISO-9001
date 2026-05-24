using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Servicio de gestion de informes de auditoria.
///
/// Se encarga de:
/// 1. Generar informes automaticamente cuando el workflow termina
/// 2. Generar informes bajo demanda (manual)
/// 3. Consultar informes existentes
///
/// No sabe nada de base de datos — le delega eso al IInformeRepository.
/// No sabe nada de HTTP — eso lo maneja el Controller.
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

    // ── CONSULTAS ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve todos los informes.
    /// Solo el Administrador puede ver todos.
    /// </summary>
    public async Task<IReadOnlyList<InformeResponse>> ObtenerTodosAsync()
    {
        var informes = await _informeRepo.ObtenerTodosAsync();
        return await MapearConDetallesAsync(informes);
    }

    /// <summary>
    /// Devuelve los informes de una auditoria especifica.
    /// </summary>
    public async Task<IReadOnlyList<InformeResponse>> ObtenerPorAuditoriaAsync(int auditoriaId)
    {
        var informes = await _informeRepo.ObtenerPorAuditoriaAsync(auditoriaId);
        return await MapearConDetallesAsync(informes);
    }

    /// <summary>
    /// Devuelve un informe por su ID.
    /// </summary>
    public async Task<InformeResponse?> ObtenerPorIdAsync(int id)
    {
        var informe = await _informeRepo.ObtenerPorIdAsync(id);
        if (informe == null) return null;

        return await MapearConDetallesAsync(informe);
    }

    // ── GENERAR INFORMES ──────────────────────────────────────────────────────

    /// <summary>
    /// Genera un informe automaticamente cuando el workflow de agentes termina.
    /// Lo llama el orquestador — no el usuario directamente.
    /// </summary>
    public async Task<InformeResponse> GenerarAutomaticoAsync(int auditoriaId)
    {
        var auditoria = await _auditoriaRepo.ObtenerPorIdAsync(auditoriaId);
        if (auditoria == null)
            throw new InvalidOperationException($"Auditoria con ID {auditoriaId} no encontrada");

        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(auditoria.ProyectoId);

        // Generamos el contenido del informe
        var contenido = GenerarContenido(auditoria, proyecto?.Nombre ?? "Proyecto no encontrado");

        var informe = new Informe
        {
            AuditoriaId     = auditoriaId,
            FechaGeneracion = DateTime.UtcNow,
            Tipo            = TiposInforme.Automatico,
            Contenido       = contenido,
            Activo          = true
        };

        var creado = await _informeRepo.CrearAsync(informe);

        _logger.LogInformation(
            "Informe automatico generado | Id={Id} | AuditoriaId={AId}",
            creado.Id, creado.AuditoriaId);

        return await MapearConDetallesAsync(creado);
    }

    /// <summary>
    /// Genera un informe bajo demanda por un usuario.
    /// Cualquier usuario autenticado puede generar un informe manual.
    /// </summary>
    public async Task<InformeResponse> GenerarManualAsync(GenerarInformeRequest request)
    {
        var auditoria = await _auditoriaRepo.ObtenerPorIdAsync(request.AuditoriaId);
        if (auditoria == null)
            throw new InvalidOperationException($"Auditoria con ID {request.AuditoriaId} no encontrada");

        // Solo se puede generar informe de auditorias completadas
        if (auditoria.Estado != EstadosAuditoria.Completada)
            throw new InvalidOperationException(
                "Solo se puede generar informe de auditorias completadas");

        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(auditoria.ProyectoId);

        var contenido = GenerarContenido(auditoria, proyecto?.Nombre ?? "Proyecto no encontrado");

        var informe = new Informe
        {
            AuditoriaId     = request.AuditoriaId,
            FechaGeneracion = DateTime.UtcNow,
            Tipo            = TiposInforme.Manual,
            Contenido       = contenido,
            Activo          = true
        };

        var creado = await _informeRepo.CrearAsync(informe);

        _logger.LogInformation(
            "Informe manual generado | Id={Id} | AuditoriaId={AId}",
            creado.Id, creado.AuditoriaId);

        return await MapearConDetallesAsync(creado);
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    /// <summary>
    /// Genera el contenido del informe en texto plano.
    /// Pendiente: definir el formato exacto segun lo que defina el cliente.
    /// Por ahora genera un resumen basico de la auditoria.
    /// </summary>
    private static string GenerarContenido(Auditoria auditoria, string nombreProyecto)
    {
        return $"""
                INFORME DE AUDITORIA ISO 9001
                =============================
                Proyecto       : {nombreProyecto}
                Auditoria ID   : {auditoria.Id}
                Etapa          : {auditoria.EtapaId}
                Fecha inicio   : {auditoria.FechaInicioUtc:yyyy-MM-dd HH:mm} UTC
                Fecha fin      : {auditoria.FechaFinalizacionUtc?.ToString("yyyy-MM-dd HH:mm") ?? "En curso"} UTC
                Estado         : {auditoria.Estado}
                Horas estimadas: {auditoria.HorasEstimadas?.ToString() ?? "No definido"}
                """;
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
            i.Tipo,
            i.Contenido,
            i.Activo);
    }
}