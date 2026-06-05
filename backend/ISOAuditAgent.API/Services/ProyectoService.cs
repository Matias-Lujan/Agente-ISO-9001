using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Servicio de gestion de proyectos.
/// Actualizado para usar el enum TipoProyecto en lugar de strings "A", "B".
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

    public async Task<IReadOnlyList<ProyectoResponse>> ObtenerTodosAsync()
    {
        var proyectos = await _proyectoRepo.ObtenerTodosAsync();
        return await MapearConResponsablesAsync(proyectos);
    }

    public async Task<IReadOnlyList<ProyectoResponse>> ObtenerPorUsuarioAsync(int usuarioId)
    {
        var proyectos = await _proyectoRepo.ObtenerPorUsuarioAsync(usuarioId);
        return await MapearConResponsablesAsync(proyectos);
    }

    public async Task<ProyectoResponse?> ObtenerPorIdAsync(int id)
    {
        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(id);
        if (proyecto == null) return null;
        var responsables = await ObtenerResponsablesAsync(id);
        return MapearAResponse(proyecto, responsables);
    }

    public async Task<ProyectoResponse> CrearAsync(CrearProyectoRequest request)
    {
        // Parseamos el string del tipo al enum TipoProyecto
        if (!Enum.TryParse<TipoProyecto>(request.TipoProyecto, out var tipoEnum))
            throw new ArgumentException($"Tipo de proyecto inválido: {request.TipoProyecto}. Debe ser A o B.");

        var proyecto = new Proyecto
        {
            Nombre            = request.Nombre,
            Descripcion       = request.Descripcion,
            FechaInicio       = request.FechaInicio ?? DateTime.UtcNow,
            FechaFin          = request.FechaFin,
            TipoProyecto      = tipoEnum,
            HorasEstimadas    = request.HorasEstimadas ?? 0,
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

    public async Task<ProyectoResponse?> ModificarAsync(int id, ModificarProyectoRequest request)
    {
        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(id);
        if (proyecto == null) return null;

        if (request.Nombre            != null) proyecto.Nombre            = request.Nombre;
        if (request.Descripcion       != null) proyecto.Descripcion       = request.Descripcion;
        if (request.FechaInicio       != null) proyecto.FechaInicio       = request.FechaInicio.Value;
        if (request.FechaFin          != null) proyecto.FechaFin          = request.FechaFin;
        if (request.HorasEstimadas    != null) proyecto.HorasEstimadas    = request.HorasEstimadas.Value;
        if (request.TrelloBoardId     != null) proyecto.TrelloBoardId     = request.TrelloBoardId;
        if (request.ClockifyProjectId != null) proyecto.ClockifyProjectId = request.ClockifyProjectId;
        if (request.DriveFolderId     != null) proyecto.DriveFolderId     = request.DriveFolderId;
        if (request.Activo            != null) proyecto.Activo            = request.Activo.Value;

        // Si viene un nuevo tipo, lo parseamos al enum
        if (request.TipoProyecto != null)
        {
            if (!Enum.TryParse<TipoProyecto>(request.TipoProyecto, out var tipoEnum))
                throw new ArgumentException($"Tipo de proyecto inválido: {request.TipoProyecto}");
            proyecto.TipoProyecto = tipoEnum;
        }

        var actualizado  = await _proyectoRepo.ActualizarAsync(proyecto);
        var responsables = await ObtenerResponsablesAsync(id);
        return MapearAResponse(actualizado, responsables);
    }

    public async Task AsignarResponsableAsync(int proyectoId, int usuarioId)
    {
        var proyecto = await _proyectoRepo.ObtenerPorIdAsync(proyectoId)
            ?? throw new InvalidOperationException($"Proyecto con ID {proyectoId} no encontrado");

        var usuario = await _usuarioRepo.ObtenerPorIdAsync(usuarioId)
            ?? throw new InvalidOperationException($"Usuario con ID {usuarioId} no encontrado");

        await _proyectoRepo.AsignarResponsableAsync(proyectoId, usuarioId);

        _logger.LogInformation(
            "Responsable asignado | ProyectoId={PId} | UsuarioId={UId}",
            proyectoId, usuarioId);
    }

    public async Task QuitarResponsableAsync(int proyectoId, int usuarioId)
    {
        await _proyectoRepo.QuitarResponsableAsync(proyectoId, usuarioId);
        _logger.LogInformation(
            "Responsable quitado | ProyectoId={PId} | UsuarioId={UId}",
            proyectoId, usuarioId);
    }

    private async Task<List<UsuarioResponse>> ObtenerResponsablesAsync(int proyectoId)
    {
        var usuarioIds   = await _proyectoRepo.ObtenerResponsablesAsync(proyectoId);
        var responsables = new List<UsuarioResponse>();

        foreach (var uid in usuarioIds)
        {
            var usuario = await _usuarioRepo.ObtenerPorIdAsync(uid);
            if (usuario != null)
                responsables.Add(new UsuarioResponse(
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Email,
                    // Convertimos el enum a string para el DTO
                    usuario.Rol.ToString(),
                    usuario.Activo,
                    usuario.FechaCreacion,
                    usuario.TemaPreferido.ToString().ToLowerInvariant()));
        }

        return responsables;
    }

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
            // Convertimos el enum a string para el DTO
            p.TipoProyecto.ToString(),
            p.HorasEstimadas,
            p.ProcedimientoId,
            p.TrelloBoardId,
            p.ClockifyProjectId,
            p.DriveFolderId,
            p.Activo,
            responsables);
}