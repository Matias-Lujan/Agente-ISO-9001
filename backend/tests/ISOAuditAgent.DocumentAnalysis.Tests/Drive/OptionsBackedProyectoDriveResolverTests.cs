using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Drive;

/// <summary>
/// Tests del resolver: cubre los criterios de aceptación de Fase 1
/// (devuelve FolderId desde appsettings; falla controladamente si no hay mapeo).
/// </summary>
public class OptionsBackedProyectoDriveResolverTests
{
    [Theory]
    [InlineData(1, "folder-uno")]
    [InlineData(42, "folder-cuarenta-y-dos")]
    public void ResolveFolderId_devuelve_FolderId_para_ProyectoId_configurado(
        int proyectoId, string esperado)
    {
        var resolver = BuildResolver(new Dictionary<int, string>
        {
            [1] = "folder-uno",
            [42] = "folder-cuarenta-y-dos"
        });

        var actual = resolver.ResolveFolderId(proyectoId);

        Assert.Equal(esperado, actual);
    }

    [Fact]
    public void ResolveFolderId_lanza_excepcion_tipada_si_no_hay_mapeo()
    {
        var resolver = BuildResolver(new Dictionary<int, string>
        {
            [1] = "folder-uno"
        });

        var ex = Assert.Throws<ProyectoDriveMappingNotFoundException>(
            () => resolver.ResolveFolderId(999));

        Assert.Equal(999, ex.ProyectoId);
        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public void TryResolveFolderId_devuelve_true_y_FolderId_si_existe_mapeo()
    {
        var resolver = BuildResolver(new Dictionary<int, string>
        {
            [7] = "folder-siete"
        });

        var encontrado = resolver.TryResolveFolderId(7, out var folderId);

        Assert.True(encontrado);
        Assert.Equal("folder-siete", folderId);
    }

    [Fact]
    public void TryResolveFolderId_devuelve_false_y_null_si_no_existe_mapeo()
    {
        var resolver = BuildResolver(new Dictionary<int, string>
        {
            [7] = "folder-siete"
        });

        var encontrado = resolver.TryResolveFolderId(99, out var folderId);

        Assert.False(encontrado);
        Assert.Null(folderId);
    }

    [Fact]
    public void Resolver_se_resuelve_via_DI_con_AddDocumentAnalysis()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DocumentAnalysis:Drive:Mappings:0:ProyectoId"] = "10",
                ["DocumentAnalysis:Drive:Mappings:0:FolderId"] = "folder-diez"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddDocumentAnalysis(configuration);
        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IProyectoDriveResolver>();

        Assert.IsType<OptionsBackedProyectoDriveResolver>(resolver);
        Assert.Equal("folder-diez", resolver.ResolveFolderId(10));
    }

    private static IProyectoDriveResolver BuildResolver(
        IReadOnlyDictionary<int, string> mappings)
    {
        var options = new DocumentAnalysisOptions
        {
            Drive = new DriveOptions
            {
                Mappings = mappings
                    .Select(kvp => new ProyectoFolderMapping
                    {
                        ProyectoId = kvp.Key,
                        FolderId = kvp.Value
                    })
                    .ToList()
            }
        };

        return new OptionsBackedProyectoDriveResolver(Options.Create(options));
    }
}
