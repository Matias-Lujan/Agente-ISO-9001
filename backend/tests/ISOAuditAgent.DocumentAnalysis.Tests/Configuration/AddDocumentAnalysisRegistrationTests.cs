using ISOAuditAgent.DocumentAnalysis;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Extensions;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Sources;
using ISOAuditAgent.DocumentAnalysis.Tests.Drive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Configuration;

/// <summary>
/// Verifica que <c>AddDocumentAnalysis</c> registra el resolver, la
/// política de exclusiones y el listing service (la implementación
/// concreta de <see cref="IDriveClient"/> la inyecta el host —
/// <c>GoogleDriveClient</c> en <c>ISOAuditAgent.API</c>).
/// </summary>
public class AddDocumentAnalysisRegistrationTests
{
    [Fact]
    public void Registra_resolver_y_policy_sin_necesitar_IDriveClient()
    {
        using var provider = BuildProvider(includeFakeDriveClient: false);

        Assert.NotNull(provider.GetService<IProyectoDriveResolver>());
        Assert.NotNull(provider.GetService<DriveExclusionPolicy>());
    }

    [Fact]
    public void IDriveListingService_se_resuelve_cuando_el_host_inyecta_un_IDriveClient()
    {
        using var provider = BuildProvider(includeFakeDriveClient: true);

        var listing = provider.GetService<IDriveListingService>();

        Assert.NotNull(listing);
    }

    [Fact]
    public void IDocumentSource_se_resuelve_cuando_el_host_inyecta_un_IDriveClient()
    {
        using var provider = BuildProvider(includeFakeDriveClient: true);

        var source = provider.GetService<IDocumentSource>();

        Assert.NotNull(source);
        Assert.IsType<DriveDocumentSource>(source);
    }

    [Fact]
    public void DocumentParserRegistry_se_resuelve_con_los_tres_parsers_PDF_DOCX_XLSX()
    {
        using var provider = BuildProvider(includeFakeDriveClient: false);

        var registry = provider.GetService<DocumentParserRegistry>();

        Assert.NotNull(registry);
        Assert.Contains("application/pdf", registry.SupportedMimeTypes);
        Assert.Contains(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            registry.SupportedMimeTypes);
        Assert.Contains(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            registry.SupportedMimeTypes);
    }

    [Fact]
    public void DocumentAnalysisRunner_se_resuelve_con_sources_y_parsers()
    {
        using var provider = BuildProvider(includeFakeDriveClient: true);

        var runner = provider.GetService<DocumentAnalysisRunner>();

        Assert.NotNull(runner);
    }

    private static ServiceProvider BuildProvider(bool includeFakeDriveClient)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DocumentAnalysis:Drive:Mappings:0:ProyectoId"] = "1",
                ["DocumentAnalysis:Drive:Mappings:0:FolderId"] = "folder-uno",
                ["DocumentAnalysis:Drive:MimeTypes:0"] = "application/pdf",
                ["DocumentAnalysis:Drive:Exclusiones:NombresExactos:0"] = "Backup"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddDocumentAnalysis(configuration);

        if (includeFakeDriveClient)
        {
            services.AddSingleton<IDriveClient>(new FakeDriveClient(
                new Dictionary<string, IReadOnlyList<DriveItem>>()));
        }

        return services.BuildServiceProvider();
    }
}
