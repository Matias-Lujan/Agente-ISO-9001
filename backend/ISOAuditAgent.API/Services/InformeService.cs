using System.Text;
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

    public async Task<InformeResponse> GenerarAutomaticoAsync(int auditoriaId, CancellationToken ct = default)
    {
        var auditoria = await _auditoriaRepo.ObtenerConResultadoOrNullAsync(auditoriaId, ct)
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
        var auditoria = await _auditoriaRepo.ObtenerConResultadoOrNullAsync(request.AuditoriaId, CancellationToken.None)
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

    // Arma el contenido del informe a partir del resultado real de la auditoría:
    // cumplimiento, hallazgos por tipo (con descripción) y artefactos evaluados.
    // Requiere que la auditoría venga cargada con ArtefactosEvaluados → Hallazgos
    // (ObtenerConResultadoOrNullAsync). Regla de cumplimiento: conformes /
    // (conformes + no conformes) entre los artefactos aplicables.
    private static string GenerarContenido(Auditoria auditoria, string nombreProyecto)
    {
        var aplicables = auditoria.ArtefactosEvaluados
            .Where(ae => ae.Aplica == EstadoAplicacionTailoring.Aplica)
            .ToList();
        int conformes   = aplicables.Count(ae => ae.Resultado == ResultadoEvaluacion.Conforme);
        int noConformes = aplicables.Count(ae => ae.Resultado == ResultadoEvaluacion.NoConforme);
        int evaluados   = conformes + noConformes;
        string cumplimiento = evaluados > 0
            ? $"{(int)Math.Round((double)conformes / evaluados * 100, MidpointRounding.AwayFromZero)}% " +
              $"({conformes}/{evaluados} artefactos conformes)"
            : "sin artefactos evaluados";

        var hallazgos = aplicables
            .SelectMany(ae => ae.Hallazgos.Select(h => (Artefacto: ae.ArtefactoEsperado, Hallazgo: h)))
            .ToList();
        int nc  = hallazgos.Count(x => x.Hallazgo.Tipo == TipoHallazgo.NC);
        int obs = hallazgos.Count(x => x.Hallazgo.Tipo == TipoHallazgo.OBS);
        int om  = hallazgos.Count(x => x.Hallazgo.Tipo == TipoHallazgo.OM);

        var sb = new StringBuilder();
        sb.AppendLine("INFORME DE AUDITORÍA ISO 9001");
        sb.AppendLine("=============================");
        sb.AppendLine($"Proyecto       : {nombreProyecto}");
        sb.AppendLine($"Auditoría ID   : {auditoria.Id}");
        sb.AppendLine($"Etapa          : {auditoria.EtapaId}");
        sb.AppendLine($"Fecha inicio   : {auditoria.FechaInicioUtc:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine($"Fecha fin      : {auditoria.FechaFinalizacionUtc?.ToString("yyyy-MM-dd HH:mm") ?? "—"} UTC");
        sb.AppendLine($"Estado         : {auditoria.Estado}");
        sb.AppendLine();
        sb.AppendLine("RESUMEN");
        sb.AppendLine("-------");
        sb.AppendLine($"Cumplimiento   : {cumplimiento}");
        sb.AppendLine($"Artefactos     : {auditoria.ArtefactosEvaluados.Count} evaluados ({aplicables.Count} aplican)");
        sb.AppendLine($"Hallazgos      : {hallazgos.Count}  (NC: {nc} · OBS: {obs} · OM: {om})");
        sb.AppendLine();

        if (hallazgos.Count > 0)
        {
            sb.AppendLine("HALLAZGOS");
            sb.AppendLine("---------");
            foreach (var (artefacto, h) in hallazgos.OrderBy(x => x.Hallazgo.Tipo))
            {
                var nombre = artefacto?.Nombre ?? "Artefacto";
                var codigo = string.IsNullOrWhiteSpace(artefacto?.Codigo) ? "" : $"[{artefacto!.Codigo}] ";
                sb.AppendLine($"- ({h.Tipo}) {codigo}{nombre}");
                sb.AppendLine($"    {h.Descripcion}");
                if (!string.IsNullOrWhiteSpace(h.Justificacion))
                    sb.AppendLine($"    Justificación: {h.Justificacion}");
                sb.AppendLine($"    Agente: {h.AgenteOrigen}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("ARTEFACTOS EVALUADOS");
        sb.AppendLine("--------------------");
        foreach (var ae in auditoria.ArtefactosEvaluados.OrderBy(a => a.Id))
        {
            var nombre = ae.ArtefactoEsperado?.Nombre ?? $"Artefacto #{ae.ArtefactoEsperadoId}";
            var codigo = string.IsNullOrWhiteSpace(ae.ArtefactoEsperado?.Codigo) ? "" : $"[{ae.ArtefactoEsperado!.Codigo}] ";
            sb.AppendLine($"- {codigo}{nombre}: {ae.Aplica} / {ae.Resultado}");
        }

        return sb.ToString();
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