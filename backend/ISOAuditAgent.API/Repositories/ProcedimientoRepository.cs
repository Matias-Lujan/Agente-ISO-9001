using ISOAuditAgent.API.Models;
using Supabase;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de procedimientos usando Supabase.
/// Es la unica clase que sabe como hablar con Supabase para
/// procedimientos, etapas y artefactos esperados.
/// </summary>
public class ProcedimientoRepository : IProcedimientoRepository
{
    private readonly Client _supabase;
    private readonly ILogger<ProcedimientoRepository> _logger;

    public ProcedimientoRepository(Client supabase, ILogger<ProcedimientoRepository> logger)
    {
        _supabase = supabase;
        _logger   = logger;
    }

    // ── PROCEDIMIENTOS ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Procedimiento>> ObtenerTodosAsync()
    {
        try
        {
            var response = await _supabase
                .From<ProcedimientoSupabase>()
                .Get();

            return response.Models.Select(MapearProcedimiento).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener procedimientos");
            return [];
        }
    }

    public async Task<Procedimiento?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var response = await _supabase
                .From<ProcedimientoSupabase>()
                .Where(p => p.Id == id)
                .Single();

            return response == null ? null : MapearProcedimiento(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener procedimiento: {Id}", id);
            return null;
        }
    }

    // ── ETAPAS ────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Etapa>> ObtenerEtapasPorProcedimientoAsync(int procedimientoId)
    {
        try
        {
            var response = await _supabase
                .From<EtapaSupabase>()
                .Where(e => e.ProcedimientoId == procedimientoId)
                .Get();

            // Ordenamos por el campo Orden para mostrarlas en secuencia
            return response.Models
                .OrderBy(e => e.Orden)
                .Select(MapearEtapa)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener etapas del procedimiento: {Id}", procedimientoId);
            return [];
        }
    }

    public async Task<Etapa?> ObtenerEtapaPorIdAsync(int id)
    {
        try
        {
            var response = await _supabase
                .From<EtapaSupabase>()
                .Where(e => e.Id == id)
                .Single();

            return response == null ? null : MapearEtapa(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener etapa: {Id}", id);
            return null;
        }
    }

    // ── ARTEFACTOS ESPERADOS ──────────────────────────────────────────────────

    public async Task<IReadOnlyList<ArtefactoEsperado>> ObtenerArtefactosPorEtapaAsync(int etapaId)
    {
        try
        {
            var response = await _supabase
                .From<ArtefactoEsperadoSupabase>()
                .Where(a => a.EtapaId == etapaId)
                .Get();

            return response.Models.Select(MapearArtefacto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener artefactos de la etapa: {Id}", etapaId);
            return [];
        }
    }

    public async Task<ArtefactoEsperado?> ObtenerArtefactoPorIdAsync(int id)
    {
        try
        {
            var response = await _supabase
                .From<ArtefactoEsperadoSupabase>()
                .Where(a => a.Id == id)
                .Single();

            return response == null ? null : MapearArtefacto(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener artefacto: {Id}", id);
            return null;
        }
    }

    public async Task<ArtefactoEsperado> CrearArtefactoAsync(ArtefactoEsperado artefacto)
    {
        var supabaseArtefacto = new ArtefactoEsperadoSupabase
        {
            EtapaId              = artefacto.EtapaId,
            Codigo               = artefacto.Codigo,
            Nombre               = artefacto.Nombre,
            Descripcion          = artefacto.Descripcion,
            MandatorioTipoA      = artefacto.MandatorioTipoA,
            MandatorioTipoB      = artefacto.MandatorioTipoB,
            PathTemplateRelativo = artefacto.PathTemplateRelativo
        };

        var response = await _supabase
            .From<ArtefactoEsperadoSupabase>()
            .Insert(supabaseArtefacto);

        return MapearArtefacto(response.Models.First());
    }

    public async Task<ArtefactoEsperado> ActualizarArtefactoAsync(ArtefactoEsperado artefacto)
    {
        var supabaseArtefacto = new ArtefactoEsperadoSupabase
        {
            Id                   = artefacto.Id,
            EtapaId              = artefacto.EtapaId,
            Codigo               = artefacto.Codigo,
            Nombre               = artefacto.Nombre,
            Descripcion          = artefacto.Descripcion,
            MandatorioTipoA      = artefacto.MandatorioTipoA,
            MandatorioTipoB      = artefacto.MandatorioTipoB,
            PathTemplateRelativo = artefacto.PathTemplateRelativo
        };

        var response = await _supabase
            .From<ArtefactoEsperadoSupabase>()
            .Where(a => a.Id == artefacto.Id)
            .Update(supabaseArtefacto);

        return MapearArtefacto(response.Models.First());
    }

    // ── Mapeos ────────────────────────────────────────────────────────────────

    private static Procedimiento MapearProcedimiento(ProcedimientoSupabase p) => new()
    {
        Id          = p.Id,
        Codigo      = p.Codigo,
        Nombre      = p.Nombre,
        Descripcion = p.Descripcion
    };

    private static Etapa MapearEtapa(EtapaSupabase e) => new()
    {
        Id               = e.Id,
        ProcedimientoId  = e.ProcedimientoId,
        Nombre           = e.Nombre,
        Orden            = e.Orden,
        Descripcion      = e.Descripcion
    };

    private static ArtefactoEsperado MapearArtefacto(ArtefactoEsperadoSupabase a) => new()
    {
        Id                   = a.Id,
        EtapaId              = a.EtapaId,
        Codigo               = a.Codigo,
        Nombre               = a.Nombre,
        Descripcion          = a.Descripcion,
        MandatorioTipoA      = a.MandatorioTipoA,
        MandatorioTipoB      = a.MandatorioTipoB,
        PathTemplateRelativo = a.PathTemplateRelativo
    };
}

// ── Modelos de Supabase ───────────────────────────────────────────────────────

[Supabase.Postgrest.Attributes.Table("procedimiento")]
public class ProcedimientoSupabase : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id")]
    public int Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("codigo")]
    public string Codigo { get; set; } = string.Empty;

    [Supabase.Postgrest.Attributes.Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Supabase.Postgrest.Attributes.Column("descripcion")]
    public string? Descripcion { get; set; }
}

[Supabase.Postgrest.Attributes.Table("etapa")]
public class EtapaSupabase : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id")]
    public int Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("procedimiento_id")]
    public int ProcedimientoId { get; set; }

    [Supabase.Postgrest.Attributes.Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Supabase.Postgrest.Attributes.Column("orden")]
    public int Orden { get; set; }

    [Supabase.Postgrest.Attributes.Column("descripcion")]
    public string? Descripcion { get; set; }
}

[Supabase.Postgrest.Attributes.Table("artefacto_esperado")]
public class ArtefactoEsperadoSupabase : Supabase.Postgrest.Models.BaseModel
{
    [Supabase.Postgrest.Attributes.PrimaryKey("id")]
    public int Id { get; set; }

    [Supabase.Postgrest.Attributes.Column("etapa_id")]
    public int EtapaId { get; set; }

    [Supabase.Postgrest.Attributes.Column("codigo")]
    public string? Codigo { get; set; }

    [Supabase.Postgrest.Attributes.Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Supabase.Postgrest.Attributes.Column("descripcion")]
    public string? Descripcion { get; set; }

    [Supabase.Postgrest.Attributes.Column("mandatorio_tipo_a")]
    public bool MandatorioTipoA { get; set; } = true;

    [Supabase.Postgrest.Attributes.Column("mandatorio_tipo_b")]
    public bool MandatorioTipoB { get; set; } = true;

    [Supabase.Postgrest.Attributes.Column("path_template_relativo")]
    public string? PathTemplateRelativo { get; set; }
}