using ISOAuditAgent.Contracts;
using ISOAuditAgent.Infrastructure.DocumentAnalysis;
using ISOAuditAgent.Infrastructure.Repositories;

using ISOAuditAgent.Infrastructure.Tests;

namespace ISOAuditAgent.Infrastructure.Tests.DocumentAnalysis;

public sealed class ContextoAuditoriaServiceTests
{
    [Fact]
    public async Task Precalcula_Exigible_solo_hasta_etapa_actual()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var svc = new ContextoAuditoriaService(
            new ProyectoRepository(db),
            new EtapaRepository(db),
            new ArtefactoEsperadoRepository(db),
            new ConfiguracionSistemaRepository(db));

        var ctx = await svc.GetContextoAuditoriaAsync(
            proyectoId: 1,
            etapaIdActual: 1);

        var fr29 = ctx.ArtefactosEsperados.Single(a => a.Codigo == "FR 29");
        var fr30 = ctx.ArtefactosEsperados.Single(a => a.Codigo == "FR 30");

        Assert.Equal(ExigibilidadArtefacto.Exigible, fr29.Exigibilidad);
        Assert.Equal(ExigibilidadArtefacto.PendienteEtapaFutura, fr30.Exigibilidad);
    }

    [Fact]
    public async Task Incluye_FolderId_templates_desde_configuracion()
    {
        using var db = InMemoryDbContextFactory.CreateWithSeed();
        var svc = new ContextoAuditoriaService(
            new ProyectoRepository(db),
            new EtapaRepository(db),
            new ArtefactoEsperadoRepository(db),
            new ConfiguracionSistemaRepository(db));

        var ctx = await svc.GetContextoAuditoriaAsync(1, etapaIdActual: 1);
        Assert.False(string.IsNullOrWhiteSpace(ctx.DriveFolderIdTemplates));
    }
}
