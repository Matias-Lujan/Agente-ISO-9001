using ISOAuditAgent.API.Models;
using Supabase;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de reglas de validacion usando Supabase.
/// Es la unica clase que sabe como hablar con Supabase para reglas.
/// </summary>
public class ReglaValidacionRepository : IReglaValidacionRepository
{
    private readonly Client _supabase;
    private readonly ILogger<ReglaValidacionRepository> _logger;

    public ReglaValidacionRepository(Client supabase, ILogger<ReglaValidacionRepository> logger)
    {
        _supabase = supabase;
        _logger   = logger;
    }

    public async Task<IReadOnlyList<ReglaValidacion>> ObtenerPorProcedimientoAsync(int procedimientoId)
    {
        try
        {
            var response = await _supabase
                .From<ReglaValidacionSupabase>()
                .Where(r => r.ProcedimientoId == procedimientoId && r.Activa == true)
                .Get();

            return response.Models.Select(MapearRegla).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reglas del procedimiento: {Id}", procedimientoId);
            return [];
        }
    }

    public async Task<IReadOnlyList<ReglaValidacion>> ObtenerPorEtapaAsync(int etapaId)
    {
        try
        {
            var response = await _supabase
                .From<ReglaValidacionSupabase>()
                .Where(r => r.EtapaId == etapaId && r.Activa == true)
                .Get();

            return response.Models.Select(MapearRegla).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reglas de la etapa: {Id}", etapaId);
            return [];
        }
    }

    public async Task<ReglaValidacion?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var response = await _supabase
                .From<ReglaValidacionSupabase>()
                .Where(r => r.Id == id)
                .Single();

            return response == null ? null : MapearRegla(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener regla: {Id}", id);
            return null;
        }
    }

    public async Task<ReglaValidacion> CrearAsync(ReglaValidacion regla)
    {
        var supabaseRegla = new ReglaValidacionSupabase
        {
            ProcedimientoId = regla.ProcedimientoId,
            EtapaId         = regla.EtapaId,
            Codigo          = regla.Codigo,
            Descripcion     = regla.Descripcion,
            Tipo            = regla.Tipo,
            Obligatoria     = regla.Obligatoria,
            Activa          = regla.Activa
        };

        var response = await _supabase
            .From<ReglaValidacionSupabase>()
            .Insert(supabaseRegla);

        return MapearRegla(response.Models.First());
    }

    public async Task<ReglaValidacion> ActualizarAsync(ReglaValidacion regla)
    {
        var supabaseRegla = new ReglaValidacionSupabase
        {
            Id              = regla.Id,
            ProcedimientoId = regla.ProcedimientoId,
            EtapaId         = regla.EtapaId,
            Codigo          = regla.Codigo,
            Descripcion     = regla.Descripcion,
            Tipo            = regla.Tipo,
            Obligatoria     = regla.Obligatoria,
            Activa          = regla.Activa
        };

        var response = await _supabase
            .From<ReglaValidacionSupabase>()
            .Where(r => r.Id == regla.Id)
            .Update(supabaseRegla);

        return MapearRegla(response.Models.First());
    }

    // ── Mapeo entre modelos ───────────────────────────────────────────────────

    private static ReglaValidacion MapearRegla(ReglaValidacionSupabase r) => new()
    {
        Id              = r.Id,
        ProcedimientoId = r.ProcedimientoId,
        EtapaId         = r.EtapaId,
        Codigo          = r.Codigo,
        Descripcion     = r.Descripcion,
        Tipo            = r.Tipo,
        Obligatoria     = r.Obligatoria,
        Activa          = r.Activa
    };
}

// ── Modelo de Supabase ────────────────────────────────────────────────────────

[Supabase.Postgrest.Attributes.Table("regla_validacion")]
public class ReglaValidacionSupabase : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id")]
    public int Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("procedimiento_id")]
    public int ProcedimientoId { get; set; }

    [Supabase.Postgrest.Attributes.Column("etapa_id")]
    public int? EtapaId { get; set; }

    [Supabase.Postgrest.Attributes.Column("codigo")]
    public string? Codigo { get; set; }

    [Supabase.Postgrest.Attributes.Column("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [Supabase.Postgrest.Attributes.Column("tipo")]
    public string Tipo { get; set; } = TiposRegla.Existencia;

    [Supabase.Postgrest.Attributes.Column("obligatoria")]
    public bool Obligatoria { get; set; } = true;

    [Supabase.Postgrest.Attributes.Column("activa")]
    public bool Activa { get; set; } = true;
}