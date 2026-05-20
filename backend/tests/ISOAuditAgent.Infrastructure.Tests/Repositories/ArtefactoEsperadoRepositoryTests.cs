using ISOAuditAgent.Infrastructure.Repositories;

namespace ISOAuditAgent.Infrastructure.Tests.Repositories;

public sealed class ArtefactoEsperadoRepositoryTests
{
    [Fact]
    public async Task ListarPorProcedimiento_carga_Etapa_eager()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new ArtefactoEsperadoRepository(db);

        var artefactos = await repo.ListarPorProcedimientoAsync(1);

        Assert.NotEmpty(artefactos);
        Assert.All(artefactos, a =>
        {
            Assert.NotNull(a.Etapa);
            Assert.True(a.Etapa!.Orden > 0);
        });
    }

    [Fact]
    public async Task ListarPorProcedimiento_ordena_por_Etapa_Orden_luego_por_Codigo()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new ArtefactoEsperadoRepository(db);

        var artefactos = await repo.ListarPorProcedimientoAsync(1);

        var ordenes = artefactos.Select(a => a.Etapa!.Orden).ToArray();
        var ordenadas = ordenes.OrderBy(x => x).ToArray();
        Assert.Equal(ordenadas, ordenes);
    }

    [Fact]
    public async Task ListarPorProcedimiento_devuelve_lista_vacia_si_no_existe_el_procedimiento()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new ArtefactoEsperadoRepository(db);

        var artefactos = await repo.ListarPorProcedimientoAsync(999);

        Assert.Empty(artefactos);
    }

    [Fact]
    public async Task ListarPorProcedimiento_incluye_FR_29_en_Planificacion()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new ArtefactoEsperadoRepository(db);

        var artefactos = await repo.ListarPorProcedimientoAsync(1);

        var fr29 = artefactos.SingleOrDefault(a => a.Codigo == "FR 29");
        Assert.NotNull(fr29);
        Assert.Equal("Planificación", fr29!.Etapa!.Nombre);
        Assert.Equal("FR-29.xlsx", fr29.TemplateDriveFilename);
    }
}
