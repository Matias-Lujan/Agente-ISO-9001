using ISOAuditAgent.API.Models;
using Supabase;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de proyectos usando Supabase.
/// Es la unica clase que sabe como hablar con Supabase para proyectos.
/// </summary>
public class ProyectoRepository : IProyectoRepository
{
    private readonly Client _supabase;
    private readonly ILogger<ProyectoRepository> _logger;

    public ProyectoRepository(Client supabase, ILogger<ProyectoRepository> logger)
    {
        _supabase = supabase;
        _logger   = logger;
    }

    public async Task<IReadOnlyList<Proyecto>> ObtenerTodosAsync()
    {
        try
        {
            var response = await _supabase
                .From<ProyectoSupabase>()
                .Where(p => p.Activo == true)
                .Get();

            return response.Models.Select(MapearAProyecto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los proyectos");
            return [];
        }
    }

    public async Task<Proyecto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var response = await _supabase
                .From<ProyectoSupabase>()
                .Where(p => p.Id == id)
                .Single();

            return response == null ? null : MapearAProyecto(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener proyecto por ID: {Id}", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<Proyecto>> ObtenerPorUsuarioAsync(int usuarioId)
    {
        try
        {
            // Primero obtenemos los IDs de proyectos asignados al usuario
            var asignaciones = await _supabase
                .From<ProyectoUsuarioSupabase>()
                .Where(pu => pu.UsuarioId == usuarioId)
                .Get();

            var proyectoIds = asignaciones.Models.Select(pu => pu.ProyectoId).ToList();

            if (!proyectoIds.Any())
                return [];

            // Luego obtenemos los proyectos correspondientes
            var proyectos = await _supabase
                .From<ProyectoSupabase>()
                .Get();

            return proyectos.Models
                .Where(p => proyectoIds.Contains(p.Id) && p.Activo)
                .Select(MapearAProyecto)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener proyectos por usuario: {Id}", usuarioId);
            return [];
        }
    }

    public async Task<Proyecto> CrearAsync(Proyecto proyecto)
    {
        var supabaseProyecto = new ProyectoSupabase
        {
            Nombre             = proyecto.Nombre,
            Descripcion        = proyecto.Descripcion,
            FechaInicio        = proyecto.FechaInicio,
            FechaFin           = proyecto.FechaFin,
            TipoProyecto       = proyecto.TipoProyecto,
            HorasEstimadas     = proyecto.HorasEstimadas,
            ProcedimientoId    = proyecto.ProcedimientoId,
            TrelloBoardId      = proyecto.TrelloBoardId,
            ClockifyProjectId  = proyecto.ClockifyProjectId,
            DriveFolderId      = proyecto.DriveFolderId,
            Activo             = proyecto.Activo
        };

        var response = await _supabase
            .From<ProyectoSupabase>()
            .Insert(supabaseProyecto);

        return MapearAProyecto(response.Models.First());
    }

    public async Task<Proyecto> ActualizarAsync(Proyecto proyecto)
    {
        var supabaseProyecto = new ProyectoSupabase
        {
            Id                 = proyecto.Id,
            Nombre             = proyecto.Nombre,
            Descripcion        = proyecto.Descripcion,
            FechaInicio        = proyecto.FechaInicio,
            FechaFin           = proyecto.FechaFin,
            TipoProyecto       = proyecto.TipoProyecto,
            HorasEstimadas     = proyecto.HorasEstimadas,
            ProcedimientoId    = proyecto.ProcedimientoId,
            TrelloBoardId      = proyecto.TrelloBoardId,
            ClockifyProjectId  = proyecto.ClockifyProjectId,
            DriveFolderId      = proyecto.DriveFolderId,
            Activo             = proyecto.Activo
        };

        var response = await _supabase
            .From<ProyectoSupabase>()
            .Where(p => p.Id == proyecto.Id)
            .Update(supabaseProyecto);

        return MapearAProyecto(response.Models.First());
    }

    public async Task AsignarResponsableAsync(int proyectoId, int usuarioId)
    {
        // Verificamos que no exista ya la asignacion
        var existente = await _supabase
            .From<ProyectoUsuarioSupabase>()
            .Where(pu => pu.ProyectoId == proyectoId && pu.UsuarioId == usuarioId)
            .Single();

        if (existente != null)
            return; // Ya estaba asignado, no hacemos nada

        await _supabase
            .From<ProyectoUsuarioSupabase>()
            .Insert(new ProyectoUsuarioSupabase
            {
                ProyectoId = proyectoId,
                UsuarioId  = usuarioId
            });
    }

    public async Task QuitarResponsableAsync(int proyectoId, int usuarioId)
    {
        await _supabase
            .From<ProyectoUsuarioSupabase>()
            .Where(pu => pu.ProyectoId == proyectoId && pu.UsuarioId == usuarioId)
            .Delete();
    }

    public async Task<IReadOnlyList<int>> ObtenerResponsablesAsync(int proyectoId)
    {
        var response = await _supabase
            .From<ProyectoUsuarioSupabase>()
            .Where(pu => pu.ProyectoId == proyectoId)
            .Get();

        return response.Models.Select(pu => pu.UsuarioId).ToList();
    }

    // ── Mapeo entre modelos ───────────────────────────────────────────────────

    private static Proyecto MapearAProyecto(ProyectoSupabase p) => new()
    {
        Id                = p.Id,
        Nombre            = p.Nombre,
        Descripcion       = p.Descripcion,
        FechaInicio       = p.FechaInicio,
        FechaFin          = p.FechaFin,
        TipoProyecto      = p.TipoProyecto,
        HorasEstimadas    = p.HorasEstimadas,
        ProcedimientoId   = p.ProcedimientoId,
        TrelloBoardId     = p.TrelloBoardId,
        ClockifyProjectId = p.ClockifyProjectId,
        DriveFolderId     = p.DriveFolderId,
        Activo            = p.Activo
    };
}

// ── Modelos de Supabase ───────────────────────────────────────────────────────

[Supabase.Postgrest.Attributes.Table("proyecto")]
public class ProyectoSupabase : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id")]
    public int Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Supabase.Postgrest.Attributes.Column("descripcion")]
    public string? Descripcion { get; set; }

    [Supabase.Postgrest.Attributes.Column("fecha_inicio")]
    public DateTime? FechaInicio { get; set; }

    [Supabase.Postgrest.Attributes.Column("fecha_fin")]
    public DateTime? FechaFin { get; set; }

    [Supabase.Postgrest.Attributes.Column("tipo_proyecto")]
    public string TipoProyecto { get; set; } = "B";

    [Supabase.Postgrest.Attributes.Column("horas_estimadas")]
    public int? HorasEstimadas { get; set; }

    [Supabase.Postgrest.Attributes.Column("procedimiento_id")]
    public int ProcedimientoId { get; set; }

    [Supabase.Postgrest.Attributes.Column("trello_board_id")]
    public string? TrelloBoardId { get; set; }

    [Supabase.Postgrest.Attributes.Column("clockify_project_id")]
    public string? ClockifyProjectId { get; set; }

    [Supabase.Postgrest.Attributes.Column("drive_folder_id")]
    public string? DriveFolderId { get; set; }

    [Supabase.Postgrest.Attributes.Column("activo")]
    public bool Activo { get; set; } = true;
}

[Supabase.Postgrest.Attributes.Table("proyecto_usuario")]
public class ProyectoUsuarioSupabase : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id")]
    public int Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("proyecto_id")]
    public int ProyectoId { get; set; }

    [Supabase.Postgrest.Attributes.Column("usuario_id")]
    public int UsuarioId { get; set; }
}