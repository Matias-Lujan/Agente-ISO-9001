using ISOAuditAgent.Infrastructure.Entities;
using ISOAuditAgent.Infrastructure.Repositories;

namespace ISOAuditAgent.Infrastructure.Tests.Repositories;

public sealed class ConfiguracionSistemaRepositoryTests
{
    [Fact]
    public async Task GetValorAsync_devuelve_el_valor_de_la_clave_seedeada()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new ConfiguracionSistemaRepository(db);

        var valor = await repo.GetValorAsync(ConfiguracionSistemaClaves.DriveCarpetaTemplatesId);

        Assert.False(string.IsNullOrWhiteSpace(valor));
    }

    [Fact]
    public async Task GetValorAsync_devuelve_null_si_la_clave_no_existe()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new ConfiguracionSistemaRepository(db);

        var valor = await repo.GetValorAsync("clave_inexistente_xyz");

        Assert.Null(valor);
    }

    [Fact]
    public async Task GetValorAsync_lanza_si_la_clave_es_vacia()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var repo = new ConfiguracionSistemaRepository(db);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => repo.GetValorAsync(""));
    }
}
