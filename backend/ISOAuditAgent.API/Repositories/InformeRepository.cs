using ISOAuditAgent.API.Models;
using Supabase;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de informes usando Supabase.
/// Es la unica clase que sabe como hablar con Supabase para informes.
/// </summary>
public class InformeRepository : IInformeRepository
{
    private readonly Client _supabase;
    private readonly ILogger<InformeRepository> _logger;

    public InformeRepository(Client supabase, ILogger<InformeRepository> logger)
    {
        _supabase = supabase;
        _logger   = logger;
    }

    public async Task<IReadOnlyList<Informe>> ObtenerTodosAsync()
    {
        try
        {
            var response = await _supabase
                .From<InformeSupabase>()
                .Where(i => i.Activo == true)
                .Get();

            return response.Models.Select(MapearInforme).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los informes");
            return [];
        }
    }

    public async Task<IReadOnlyList<Informe>> ObtenerPorAuditoriaAsync(int auditoriaId)
    {
        try
        {
            var response = await _supabase
                .From<InformeSupabase>()
                .Where(i => i.AuditoriaId == auditoriaId)
                .Get();

            return response.Models.Select(MapearInforme).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener informes de auditoria: {Id}", auditoriaId);
            return [];
        }
    }

    public async Task<Informe?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var response = await _supabase
                .From<InformeSupabase>()
                .Where(i => i.Id == id)
                .Single();

            return response == null ? null : MapearInforme(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener informe: {Id}", id);
            return null;
        }
    }

    public async Task<Informe> CrearAsync(Informe informe)
    {
        var supabaseInforme = new InformeSupabase
        {
            AuditoriaId      = informe.AuditoriaId,
            FechaGeneracion  = informe.FechaGeneracion,
            Tipo             = informe.Tipo,
            Contenido        = informe.Contenido,
            Activo           = informe.Activo
        };

        var response = await _supabase
            .From<InformeSupabase>()
            .Insert(supabaseInforme);

        return MapearInforme(response.Models.First());
    }

    public async Task<Informe> ActualizarAsync(Informe informe)
    {
        var supabaseInforme = new InformeSupabase
        {
            Id              = informe.Id,
            AuditoriaId     = informe.AuditoriaId,
            FechaGeneracion = informe.FechaGeneracion,
            Tipo            = informe.Tipo,
            Contenido       = informe.Contenido,
            Activo          = informe.Activo
        };

        var response = await _supabase
            .From<InformeSupabase>()
            .Where(i => i.Id == informe.Id)
            .Update(supabaseInforme);

        return MapearInforme(response.Models.First());
    }

    // ── Mapeo entre modelos ───────────────────────────────────────────────────

    private static Informe MapearInforme(InformeSupabase i) => new()
    {
        Id              = i.Id,
        AuditoriaId     = i.AuditoriaId,
        FechaGeneracion = i.FechaGeneracion,
        Tipo            = i.Tipo,
        Contenido       = i.Contenido,
        Activo          = i.Activo
    };
}

// ── Modelo de Supabase ────────────────────────────────────────────────────────

[Supabase.Postgrest.Attributes.Table("informe")]
public class InformeSupabase : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id")]
    public int Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("auditoria_id")]
    public int AuditoriaId { get; set; }

    [Supabase.Postgrest.Attributes.Column("fecha_generacion")]
    public DateTime FechaGeneracion { get; set; }

    [Supabase.Postgrest.Attributes.Column("tipo")]
    public string Tipo { get; set; } = TiposInforme.Automatico;

    [Supabase.Postgrest.Attributes.Column("contenido")]
    public string? Contenido { get; set; }

    [Supabase.Postgrest.Attributes.Column("activo")]
    public bool Activo { get; set; } = true;
}