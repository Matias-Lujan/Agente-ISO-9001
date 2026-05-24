using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Nodo 1 del workflow de auditoria — ResolutorContexto.
///
/// Es un nodo DETERMINISTA — no usa IA, solo lee la BD.
/// Su responsabilidad es armar el ContextoAuditoria completo
/// que necesitan todos los agentes para ejecutar la auditoria.
///
/// Recibe: ProyectoId, EtapaId, UsuarioId
/// Produce: ContextoAuditoria con todos los datos necesarios
///
/// Lo consume el orquestador para arrancar el workflow.
/// </summary>
public class ResolutorContextoService
{
    private readonly IProyectoRepository _proyectoRepo;
    private readonly IProcedimientoRepository _procedimientoRepo;
    private readonly IReglaValidacionRepository _reglaRepo;
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly ILogger<ResolutorContextoService> _logger;

    public ResolutorContextoService(
        IProyectoRepository proyectoRepo,
        IProcedimientoRepository procedimientoRepo,
        IReglaValidacionRepository reglaRepo,
        IAuditoriaRepository auditoriaRepo,
        ILogger<ResolutorContextoService> logger)
    {
        _proyectoRepo      = proyectoRepo;
        _procedimientoRepo = procedimientoRepo;
        _reglaRepo         = reglaRepo;
        _auditoriaRepo     = auditoriaRepo;
        _logger            = logger;
    }

    /// <summary>
    /// Arma el contexto completo de la auditoria.
    /// 
    /// Pasos:
    /// 1. Lee el proyecto de la BD
    /// 2. Lee la etapa de la BD
    /// 3. Lee los artefactos esperados de esa etapa
    /// 4. Lee las reglas de validacion de esa etapa
    /// 5. Crea la auditoria en la BD con estado "en_curso"
    /// 6. Devuelve el ContextoAuditoria completo
    /// </summary>
    public async Task<ContextoAuditoria> ResolverAsync(
        IniciarAuditoriaRequest request,
        int usuarioId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ResolutorContexto iniciado | ProyectoId={PId} | EtapaId={EId} | UsuarioId={UId}",
            request.ProyectoId, request.EtapaId, usuarioId);

        // ── Paso 1: Leer el proyecto ──────────────────────────────────────────
        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(request.ProyectoId)
            ?? throw new InvalidOperationException(
                $"Proyecto con ID {request.ProyectoId} no encontrado");

        // ── Paso 2: Leer la etapa ─────────────────────────────────────────────
        var etapa = await _procedimientoRepo.ObtenerEtapaPorIdAsync(request.EtapaId)
            ?? throw new InvalidOperationException(
                $"Etapa con ID {request.EtapaId} no encontrada");

        // ── Paso 3: Leer los artefactos esperados de la etapa ────────────────
        var artefactos = await _procedimientoRepo.ObtenerArtefactosPorEtapaAsync(request.EtapaId);

        // Determinamos si cada artefacto es obligatorio segun el tipo de proyecto
        var artefactosContexto = artefactos.Select(a => new ArtefactoContextoDto(
            a.Id,
            a.Codigo,
            a.Nombre,
            // Si el proyecto es tipo A usamos MandatorioTipoA, si es B usamos MandatorioTipoB
            EsObligatorio: proyecto.TipoProyecto == TiposProyecto.A
                ? a.MandatorioTipoA
                : a.MandatorioTipoB,
            a.PathTemplateRelativo
        )).ToList();

        // ── Paso 4: Leer las reglas de validacion de la etapa ────────────────
        var reglas = await _reglaRepo.ObtenerPorEtapaAsync(request.EtapaId);

        var reglasContexto = reglas.Select(r => new ReglaContextoDto(
            r.Id,
            r.Codigo,
            r.Descripcion,
            r.Tipo,
            r.Obligatoria
        )).ToList();

        // ── Paso 5: Crear la auditoria en la BD ──────────────────────────────
        var auditoria = new Auditoria
        {
            ProyectoId     = request.ProyectoId,
            UsuarioId      = usuarioId,
            EtapaId        = request.EtapaId,
            FechaInicioUtc = DateTime.UtcNow,
            HorasEstimadas = proyecto.HorasEstimadas,
            Estado         = EstadosAuditoria.EnCurso,
            Activo         = true
        };

        var auditoriaCreada = await _auditoriaRepo.CrearAsync(auditoria);

        _logger.LogInformation(
            "ResolutorContexto completado | AuditoriaId={AId} | Artefactos={AC} | Reglas={RC}",
            auditoriaCreada.Id, artefactosContexto.Count, reglasContexto.Count);

        // ── Paso 6: Armar y devolver el ContextoAuditoria ────────────────────
        return new ContextoAuditoria(
            AuditoriaId:        auditoriaCreada.Id,
            ProyectoId:         proyecto.Id,
            EtapaId:            etapa.Id,
            UsuarioId:          usuarioId,
            NombreProyecto:     proyecto.Nombre,
            TipoProyecto:       proyecto.TipoProyecto,
            HorasEstimadas:     proyecto.HorasEstimadas,
            ProcedimientoId:    proyecto.ProcedimientoId,
            TrelloBoardId:      proyecto.TrelloBoardId,
            ClockifyProjectId:  proyecto.ClockifyProjectId,
            DriveFolderId:      proyecto.DriveFolderId,
            NombreEtapa:        etapa.Nombre,
            OrdenEtapa:         etapa.Orden,
            ArtefactosEsperados: artefactosContexto,
            ReglasValidacion:   reglasContexto
        );
    }
}