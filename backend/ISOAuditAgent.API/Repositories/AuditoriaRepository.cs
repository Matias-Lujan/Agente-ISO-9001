using ISOAuditAgent.API.Models;
using Supabase;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de auditorias usando Supabase.
/// Es la unica clase que sabe como hablar con Supabase para auditorias.
/// </summary>
public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly Client _supabase;
    private readonly ILogger<AuditoriaRepository> _logger;

    public AuditoriaRepository(Client supabase, ILogger<AuditoriaRepository> logger)
    {
        _supabase = supabase;
        _logger   = logger;
    }

    public async Task<IReadOnlyList<Auditoria>> ObtenerTodasAsync()
    {
        try
        {
            var response = await _supabase
                .From<AuditoriaSupabase>()
                .Where(a => a.Activo == true)
                .Get();

            return response.Models.Select(MapearAuditoria).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las auditorias");
            return [];
        }
    }

    public async Task<IReadOnlyList<Auditoria>> ObtenerPorProyectoAsync(int proyectoId)
    {
        try
        {
            var response = await _supabase
                .From<AuditoriaSupabase>()
                .Where(a => a.ProyectoId == proyectoId)
                .Get();

            return response.Models.Select(MapearAuditoria).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener auditorias del proyecto: {Id}", proyectoId);
            return [];
        }
    }

    public async Task<Auditoria?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var response = await _supabase
                .From<AuditoriaSupabase>()
                .Where(a => a.Id == id)
                .Single();

            return response == null ? null : MapearAuditoria(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener auditoria por ID: {Id}", id);
            return null;
        }
    }

    public async Task<Auditoria> CrearAsync(Auditoria auditoria)
    {
        var supabaseAuditoria = new AuditoriaSupabase
        {
            ProyectoId           = auditoria.ProyectoId,
            UsuarioId            = auditoria.UsuarioId,
            EtapaId              = auditoria.EtapaId,
            FechaInicioUtc       = auditoria.FechaInicioUtc,
            FechaFinalizacionUtc = auditoria.FechaFinalizacionUtc,
            HorasEstimadas       = auditoria.HorasEstimadas,
            Estado               = auditoria.Estado,
            Activo               = auditoria.Activo
        };

        var response = await _supabase
            .From<AuditoriaSupabase>()
            .Insert(supabaseAuditoria);

        return MapearAuditoria(response.Models.First());
    }

    public async Task<Auditoria> ActualizarAsync(Auditoria auditoria)
    {
        var supabaseAuditoria = new AuditoriaSupabase
        {
            Id                   = auditoria.Id,
            ProyectoId           = auditoria.ProyectoId,
            UsuarioId            = auditoria.UsuarioId,
            EtapaId              = auditoria.EtapaId,
            FechaInicioUtc       = auditoria.FechaInicioUtc,
            FechaFinalizacionUtc = auditoria.FechaFinalizacionUtc,
            HorasEstimadas       = auditoria.HorasEstimadas,
            Estado               = auditoria.Estado,
            Activo               = auditoria.Activo
        };

        var response = await _supabase
            .From<AuditoriaSupabase>()
            .Where(a => a.Id == auditoria.Id)
            .Update(supabaseAuditoria);

        return MapearAuditoria(response.Models.First());
    }

    // ── Mapeo entre modelos ───────────────────────────────────────────────────

    private static Auditoria MapearAuditoria(AuditoriaSupabase a) => new()
    {
        Id                   = a.Id,
        ProyectoId           = a.ProyectoId,
        UsuarioId            = a.UsuarioId,
        EtapaId              = a.EtapaId,
        FechaInicioUtc       = a.FechaInicioUtc,
        FechaFinalizacionUtc = a.FechaFinalizacionUtc,
        HorasEstimadas       = a.HorasEstimadas,
        Estado               = a.Estado,
        Activo               = a.Activo
    };
}

// ── Modelo de Supabase ────────────────────────────────────────────────────────

[Supabase.Postgrest.Attributes.Table("auditoria")]
public class AuditoriaSupabase : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id")]
    public int Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("proyecto_id")]
    public int ProyectoId { get; set; }

    [Supabase.Postgrest.Attributes.Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Supabase.Postgrest.Attributes.Column("etapa_id")]
    public int EtapaId { get; set; }

    [Supabase.Postgrest.Attributes.Column("fecha_inicio_utc")]
    public DateTime FechaInicioUtc { get; set; }

    [Supabase.Postgrest.Attributes.Column("fecha_finalizacion_utc")]
    public DateTime? FechaFinalizacionUtc { get; set; }

    [Supabase.Postgrest.Attributes.Column("horas_estimadas")]
    public int? HorasEstimadas { get; set; }

    [Supabase.Postgrest.Attributes.Column("estado")]
    public string Estado { get; set; } = EstadosAuditoria.EnCurso;

    [Supabase.Postgrest.Attributes.Column("activo")]
    public bool Activo { get; set; } = true;
}