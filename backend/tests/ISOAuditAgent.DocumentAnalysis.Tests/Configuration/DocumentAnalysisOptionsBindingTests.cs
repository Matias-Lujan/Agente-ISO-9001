using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Configuration;

/// <summary>
/// Tests del binding de <see cref="DocumentAnalysisOptions"/> y de la
/// validación encadenada (DataAnnotations + IValidateOptions).
/// </summary>
public class DocumentAnalysisOptionsBindingTests
{
    [Fact]
    public void Bindea_seccion_DocumentAnalysis_completa_desde_configuracion()
    {
        var configuration = BuildConfig(new Dictionary<string, string?>
        {
            ["DocumentAnalysis:Drive:Mappings:0:ProyectoId"] = "1",
            ["DocumentAnalysis:Drive:Mappings:0:FolderId"] = "1AbCdEfGh",
            ["DocumentAnalysis:Drive:Mappings:1:ProyectoId"] = "42",
            ["DocumentAnalysis:Drive:Mappings:1:FolderId"] = "9XyZ",
            ["DocumentAnalysis:Drive:MimeTypes:0"] = "application/pdf",
            ["DocumentAnalysis:Drive:MimeTypes:1"] =
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ["DocumentAnalysis:Drive:Exclusiones:NombresExactos:0"] = "Archivo",
            ["DocumentAnalysis:Drive:Exclusiones:NombresExactos:1"] = "Backup",
            ["DocumentAnalysis:Drive:Exclusiones:PatronesRegex:0"] = ".*archivo.*",
            ["DocumentAnalysis:Drive:Exclusiones:PatronesRegex:1"] = ".*backup.*"
        });

        var options = ResolveOptions(configuration);

        Assert.Equal(2, options.Drive.Mappings.Count);
        Assert.Contains(options.Drive.Mappings,
            m => m.ProyectoId == 1 && m.FolderId == "1AbCdEfGh");
        Assert.Contains(options.Drive.Mappings,
            m => m.ProyectoId == 42 && m.FolderId == "9XyZ");

        Assert.Equal(2, options.Drive.MimeTypes.Count);
        Assert.Contains("application/pdf", options.Drive.MimeTypes);

        Assert.Equal(new[] { "Archivo", "Backup" }, options.Drive.Exclusiones.NombresExactos);
        Assert.Equal(new[] { ".*archivo.*", ".*backup.*" },
            options.Drive.Exclusiones.PatronesRegex);
    }

    [Fact]
    public void Acepta_seccion_DocumentAnalysis_vacia_con_listas_vacias_por_defecto()
    {
        var configuration = BuildConfig(new Dictionary<string, string?>());

        var options = ResolveOptions(configuration);

        Assert.NotNull(options.Drive);
        Assert.Empty(options.Drive.Mappings);
        Assert.Empty(options.Drive.MimeTypes);
        Assert.Empty(options.Drive.Exclusiones.NombresExactos);
        Assert.Empty(options.Drive.Exclusiones.PatronesRegex);
    }

    [Fact]
    public void Falla_si_FolderId_es_vacio_via_validador_estructural()
    {
        var configuration = BuildConfig(new Dictionary<string, string?>
        {
            ["DocumentAnalysis:Drive:Mappings:0:ProyectoId"] = "7",
            ["DocumentAnalysis:Drive:Mappings:0:FolderId"] = ""
        });

        var ex = Assert.Throws<OptionsValidationException>(() => ResolveOptions(configuration));
        Assert.Contains("FolderId", ex.Message);
    }

    [Fact]
    public void Falla_si_ProyectoId_es_cero_o_negativo_via_validador_estructural()
    {
        var configuration = BuildConfig(new Dictionary<string, string?>
        {
            ["DocumentAnalysis:Drive:Mappings:0:ProyectoId"] = "0",
            ["DocumentAnalysis:Drive:Mappings:0:FolderId"] = "anything"
        });

        var ex = Assert.Throws<OptionsValidationException>(() => ResolveOptions(configuration));
        Assert.Contains("ProyectoId", ex.Message);
    }

    [Fact]
    public void Falla_si_hay_ProyectoId_duplicado_via_validador_estructural()
    {
        var configuration = BuildConfig(new Dictionary<string, string?>
        {
            ["DocumentAnalysis:Drive:Mappings:0:ProyectoId"] = "1",
            ["DocumentAnalysis:Drive:Mappings:0:FolderId"] = "folder-a",
            ["DocumentAnalysis:Drive:Mappings:1:ProyectoId"] = "1",
            ["DocumentAnalysis:Drive:Mappings:1:FolderId"] = "folder-b"
        });

        var ex = Assert.Throws<OptionsValidationException>(() => ResolveOptions(configuration));
        Assert.Contains("duplicados", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Falla_si_un_PatronRegex_no_compila_via_validador_estructural()
    {
        var configuration = BuildConfig(new Dictionary<string, string?>
        {
            ["DocumentAnalysis:Drive:Exclusiones:PatronesRegex:0"] = "[invalid("
        });

        var ex = Assert.Throws<OptionsValidationException>(() => ResolveOptions(configuration));
        Assert.Contains("PatronesRegex", ex.Message);
    }

    private static IConfiguration BuildConfig(IDictionary<string, string?> entries)
        => new ConfigurationBuilder().AddInMemoryCollection(entries).Build();

    private static DocumentAnalysisOptions ResolveOptions(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddDocumentAnalysis(configuration);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<DocumentAnalysisOptions>>().Value;
    }
}
