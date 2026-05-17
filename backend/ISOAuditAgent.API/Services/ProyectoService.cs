using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Servicio de gestion de proyectos.
///
/// Se encarga de:
/// 1. Crear y modificar proyectos
/// 2. Listar proyectos (todos o por usuario)
/// 3. Asignar y quitar responsables
///
/// No sabe nada de base de datos — le delega eso al IProyectoRepository.
/// No sabe nada de HTTP — eso lo maneja el Controller.
/// </summary>
public class ProyectoService
{
    private readonly IProyectoRepository _proyectoRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ILogger<ProyectoService> _logger;

    public ProyectoService(
        IProyectoRepository proyectoRepo,
        IUsuarioRepository usuarioRepo,
        ILogger<ProyectoService> logger)
    {
        _proyectoRepo = proyectoRepo;
        _usuarioRepo  = usuarioRepo;
        _logger       = logger;
    }

    // ── CONSULTAS ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve todos los proyectos activos con sus responsables.
    /// Solo el Administrador puede ver todos los proyectos.
    /// </summary>
    public async Task<IReadOnlyList<ProyectoResponse>> ObtenerTodosAsync()
    {
        var proyectos = await _proyectoRepo.ObtenerTodosAsync();
        return await MapearConResponsablesAsync(proyectos);
    }

    /// <summary>
    /// Devuelve los proyectos asignados a un usuario especifico.
    /// Cada usuario ve solo sus proyectos.
    /// </summary>
    public async Task<IReadOnlyList<ProyectoResponse>> ObtenerPorUsuarioAsync(int usuarioId)
    {
        var proyectos = await _proyectoRepo.ObtenerPorUsuarioAsync(usuarioId);
        return await MapearConResponsablesAsync(proyectos);
    }

    /// <summary>
    /// Devuelve un proyecto por su ID con sus responsables.
    /// </summary>
    public async Task<ProyectoResponse?> ObtenerPorIdAsync(int id)
    {
        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(id);
        if (proyecto == null) return null;

        var responsables = await ObtenerResponsablesAsync(id);
        return MapearAResponse(proyecto, responsables);
    }

    // ── CREAR Y MODIFICAR ─────────────────────────────────────────────────────

    /// <summary>
    /// Crea un proyecto nuevo.
    /// Valida que el tipo de proyecto sea A o B.
    /// </summary>
    public async Task<ProyectoResponse> CrearAsync(CrearProyectoRequest request)
    {
        // Validar tipo de proyecto
        if (request.TipoProyecto != TiposProyecto.A &&
            request.TipoProyecto != TiposProyecto.B)
            throw new ArgumentException($"Tipo de proyecto invalido: {request.TipoProyecto}. Debe ser A o B.");

        var proyecto = new Proyecto
        {
            Nombre            = request.Nombre,
            Descripcion       = request.Descripcion,
            FechaInicio       = request.FechaInicio,
            FechaFin          = request.FechaFin,
            TipoProyecto      = request.TipoProyecto,
            HorasEstimadas    = request.HorasEstimadas,
            ProcedimientoId   = request.ProcedimientoId,
            TrelloBoardId     = request.TrelloBoardId,
            ClockifyProjectId = request.ClockifyProjectId,
            DriveFolderId     = request.DriveFolderId,
            Activo            = true
        };

        var creado = await _proyectoRepo.CrearAsync(proyecto);

        _logger.LogInformation(
            "Proyecto creado | Id={Id} | Nombre={Nombre} | Tipo={Tipo}",
            creado.Id, creado.Nombre, creado.TipoProyecto);

        return MapearAResponse(creado, []);
    }

    /// <summary>
    /// Modifica los datos de un proyecto existente.
    /// Solo actualiza los campos que vienen con valor.
    /// </summary>
    public async Task<ProyectoResponse?> ModificarAsync(int id, ModificarProyectoRequest request)
    {
        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(id);
        if (proyecto == null) return null;

        // Solo actualizamos los campos que vienen con valor
        if (request.Nombre            != null) proyecto.Nombre            = request.Nombre;
        if (request.Descripcion       != null) proyecto.Descripcion       = request.Descripcion;
        if (request.FechaInicio       != null) proyecto.FechaInicio       = request.FechaInicio;
        if (request.FechaFin          != null) proyecto.FechaFin          = request.FechaFin;
        if (request.TipoProyecto      != null) proyecto.TipoProyecto      = request.TipoProyecto;
        if (request.HorasEstimadas    != null) proyecto.HorasEstimadas    = request.HorasEstimadas;
        if (request.TrelloBoardId     != null) proyecto.TrelloBoardId     = request.TrelloBoardId;
        if (request.ClockifyProjectId != null) proyecto.ClockifyProjectId = request.ClockifyProjectId;
        if (request.DriveFolderId     != null) proyecto.DriveFolderId     = request.DriveFolderId;
        if (request.Activo            != null) proyecto.Activo            = request.Activo.Value;

        var actualizado = await _proyectoRepo.ActualizarAsync(proyecto);
        var responsables = await ObtenerResponsablesAsync(id);

        return MapearAResponse(actualizado, responsables);
    }

    // ── RESPONSABLES ──────────────────────────────────────────────────────────

    /// <summary>
    /// Asigna un usuario como responsable de un proyecto.
    /// Verifica que tanto el proyecto como el usuario existan.
    /// </summary>
    public async Task AsignarResponsableAsync(int proyectoId, int usuarioId)
    {
        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(proyectoId);
        if (proyecto == null)
            throw new InvalidOperationException($"Proyecto con ID {proyectoId} no encontrado");

        var usuario = await _usuarioRepo.ObtenerPorIdAsync(usuarioId);
        if (usuario == null)
            throw new InvalidOperationException($"Usuario con ID {usuarioId} no encontrado");

        await _proyectoRepo.AsignarResponsableAsync(proyectoId, usuarioId);

        _logger.LogInformation(
            "Responsable asignado | ProyectoId={PId} | UsuarioId={UId}",
            proyectoId, usuarioId);
    }

    /// <summary>
    /// Quita un usuario como responsable de un proyecto.
    /// </summary>
    public async Task QuitarResponsableAsync(int proyectoId, int usuarioId)
    {
        await _proyectoRepo.QuitarResponsableAsync(proyectoId, usuarioId);

        _logger.LogInformation(
            "Responsable quitado | ProyectoId={PId} | UsuarioId={UId}",
            proyectoId, usuarioId);
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene los datos completos de los responsables de un proyecto.
    /// </summary>
    private async Task<List<UsuarioResponse>> ObtenerResponsablesAsync(int proyectoId)
    {
        var usuarioIds = await _proyectoRepo.ObtenerResponsablesAsync(proyectoId);
        var responsables = new List<UsuarioResponse>();

        foreach (var uid in usuarioIds)
        {
            var usuario = await _usuarioRepo.ObtenerPorIdAsync(uid);
            if (usuario != null)
                responsables.Add(new UsuarioResponse(
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Email,
                    usuario.Rol,
                    usuario.Activo,
                    usuario.FechaCreacion));
        }

        return responsables;
    }

    /// <summary>
    /// Convierte una lista de proyectos a responses con sus responsables.
    /// </summary>
    private async Task<IReadOnlyList<ProyectoResponse>> MapearConResponsablesAsync(
        IReadOnlyList<Proyecto> proyectos)
    {
        var resultado = new List<ProyectoResponse>();
        foreach (var p in proyectos)
        {
            var responsables = await ObtenerResponsablesAsync(p.Id);
            resultado.Add(MapearAResponse(p, responsables));
        }
        return resultado;
    }

    private static ProyectoResponse MapearAResponse(Proyecto p, List<UsuarioResponse> responsables) =>
        new(
            p.Id,
            p.Nombre,
            p.Descripcion,
            p.FechaInicio,
            p.FechaFin,
            p.TipoProyecto,
            p.HorasEstimadas,
            p.ProcedimientoId,
            p.TrelloBoardId,
            p.ClockifyProjectId,
            p.DriveFolderId,
            p.Activo,
            responsables);
}