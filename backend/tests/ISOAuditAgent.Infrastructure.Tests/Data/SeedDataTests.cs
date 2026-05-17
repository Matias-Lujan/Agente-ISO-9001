using ISOAuditAgent.Contracts;
using ISOAuditAgent.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.Infrastructure.Tests.Data;

/// <summary>
/// Verifica que el seed mínimo declarado en <c>SeedData.Apply</c> se
/// aplica al crear la BD (con InMemory provider) y deja los datos
/// esperados para que el agente pueda probar el cruce
/// tailoring × artefactos esperados.
/// </summary>
public sealed class SeedDataTests
{
    [Fact]
    public void Seed_carga_el_procedimiento_PR_11_13()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();

        var procedimiento = db.Procedimientos.Single();

        Assert.Equal("PR 11-13", procedimiento.Codigo);
        Assert.Contains("Desarrollo", procedimiento.Nombre, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Seed_carga_las_6_etapas_del_ciclo_de_vida_en_orden()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();

        var etapas = db.Etapas.OrderBy(e => e.Orden).ToList();

        Assert.Equal(6, etapas.Count);
        Assert.Equal(
            new[]
            {
                "Planificación",
                "Análisis y Diseño",
                "Desarrollo",
                "Testing",
                "Implementación",
                "Seguimiento"
            },
            etapas.Select(e => e.Nombre).ToArray());
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, etapas.Select(e => e.Orden).ToArray());
    }

    [Fact]
    public void Seed_carga_4_artefactos_de_muestra_con_sus_codigos_FR()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();

        var artefactos = db.ArtefactosEsperados
            .Include(a => a.Etapa)
            .OrderBy(a => a.Etapa!.Orden)
            .ToList();

        Assert.Equal(4, artefactos.Count);

        var codigos = artefactos.Select(a => a.Codigo).ToArray();
        Assert.Contains("FR 29", codigos);
        Assert.Contains("FR 30", codigos);
        Assert.Contains("FR 32", codigos);
        Assert.Contains("FR 48", codigos);
    }

    [Fact]
    public void Seed_marca_FR_30_y_FR_32_como_mandatorios_solo_para_tipo_A()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();

        var fr30 = db.ArtefactosEsperados.Single(a => a.Codigo == "FR 30");
        var fr32 = db.ArtefactosEsperados.Single(a => a.Codigo == "FR 32");

        Assert.True(fr30.MandatorioTipoA);
        Assert.False(fr30.MandatorioTipoB);
        Assert.True(fr32.MandatorioTipoA);
        Assert.False(fr32.MandatorioTipoB);
    }

    [Fact]
    public void Seed_FR_29_y_FR_48_son_mandatorios_para_ambos_tipos_de_proyecto()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();

        var fr29 = db.ArtefactosEsperados.Single(a => a.Codigo == "FR 29");
        var fr48 = db.ArtefactosEsperados.Single(a => a.Codigo == "FR 48");

        Assert.True(fr29.MandatorioTipoA);
        Assert.True(fr29.MandatorioTipoB);
        Assert.True(fr48.MandatorioTipoA);
        Assert.True(fr48.MandatorioTipoB);
    }

    [Fact]
    public void Seed_carga_proyecto_de_prueba_tipo_A_con_DriveFolderId()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();

        var proyecto = db.Proyectos.Single();

        Assert.Equal(1, proyecto.Id);
        Assert.Equal(TipoProyecto.A, proyecto.TipoProyecto);
        Assert.True(proyecto.Activo);
        Assert.False(string.IsNullOrWhiteSpace(proyecto.DriveFolderId));
    }

    [Fact]
    public void Seed_carga_configuracion_drive_carpeta_templates_id()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();

        var config = db.ConfiguracionesSistema.Single();

        Assert.Equal(ConfiguracionSistemaClaves.DriveCarpetaTemplatesId, config.Clave);
        Assert.False(string.IsNullOrWhiteSpace(config.Valor));
    }
}
