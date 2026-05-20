using ISOAuditAgent.Infrastructure.Repositories;

namespace ISOAuditAgent.Infrastructure.Tests.Repositories;

public sealed class EtapaRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_devuelve_la_etapa_seedeada()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new EtapaRepository(db);

        var etapa = await repo.GetByIdAsync(1);

        Assert.NotNull(etapa);
        Assert.Equal("Planificación", etapa!.Nombre);
        Assert.Equal(1, etapa.Orden);
    }

    [Fact]
    public async Task GetByIdAsync_devuelve_null_si_la_etapa_no_existe()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new EtapaRepository(db);

        var etapa = await repo.GetByIdAsync(999);

        Assert.Null(etapa);
    }

    [Fact]
    public async Task ListarPorProcedimiento_ordena_por_Orden_ascendente()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new EtapaRepository(db);

        var etapas = await repo.ListarPorProcedimientoAsync(1);

        Assert.Equal(6, etapas.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, etapas.Select(e => e.Orden).ToArray());
    }

    [Fact]
    public async Task ListarPorProcedimiento_no_devuelve_etapas_de_otros_procedimientos()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new EtapaRepository(db);

        var etapas = await repo.ListarPorProcedimientoAsync(99);

        Assert.Empty(etapas);
    }
}
