using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Extensions;
using ISOAuditAgent.DocumentAnalysis.Mcp;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Sources;
using ISOAuditAgent.DocumentAnalysis.Tailoring;
using ISOAuditAgent.DocumentAnalysis.Tests.Drive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Configuration;

/// <summary>
/// Verifica que <c>AddDocumentAnalysis</c> registra resolver, política,
/// listing (para el servidor MCP), parsers y <see cref="IDriveMcpClient"/>.
/// <see cref="IDriveClient"/> lo registra el host para
/// <c>DriveMcpTools</c> — el agente usa MCP.
/// </summary>
public class AddDocumentAnalysisRegistrationTests
{
    [Fact]
    public void Registra_resolver_y_policy_sin_necesitar_IDriveClient()
    {
        using var provider = BuildProvider(registerDriveClient: false, registerFakeMcp: false);

        Assert.NotNull(provider.GetService<IProyectoDriveResolver>());
        Assert.NotNull(provider.GetService<DriveExclusionPolicy>());
    }

    [Fact]
    public void IDriveListingService_se_resuelve_cuando_el_host_inyecta_un_IDriveClient()
    {
        using var provider = BuildProvider(registerDriveClient: true, registerFakeMcp: false);

        var listing = provider.GetService<IDriveListingService>();

        Assert.NotNull(listing);
    }

    [Fact]
    public void IDocumentSource_se_resuelve_cuando_hay_IDriveClient_y_fake_MCP()
    {
        using var provider = BuildProvider(registerDriveClient: true, registerFakeMcp: true);

        var source = provider.GetService<IDocumentSource>();

        Assert.NotNull(source);
        Assert.IsType<DriveDocumentSource>(source);
    }

    [Fact]
    public void ITailoringReader_y_XlsxTableExtractor_se_resuelven_cuando_hay_Drive_y_fake_MCP()
    {
        using var provider = BuildProvider(registerDriveClient: true, registerFakeMcp: true);

        Assert.NotNull(provider.GetService<XlsxTableExtractor>());
        Assert.NotNull(provider.GetService<ITailoringReader>());
        Assert.IsType<TailoringReader>(provider.GetService<ITailoringReader>());
    }

    [Fact]
    public void DocumentParserRegistry_se_resuelve_con_los_tres_parsers_PDF_DOCX_XLSX()
    {
        using var provider = BuildProvider(registerDriveClient: false, registerFakeMcp: false);

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

    private static ServiceProvider BuildProvider(bool registerDriveClient, bool registerFakeMcp)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpDrive:Endpoint"] = "http://localhost:5180/mcp/drive",
                ["DocumentAnalysis:Drive:Mappings:0:ProyectoId"] = "1",
                ["DocumentAnalysis:Drive:Mappings:0:FolderId"] = "folder-uno",
                ["DocumentAnalysis:Drive:MimeTypes:0"] = "application/pdf",
                ["DocumentAnalysis:Drive:Exclusiones:NombresExactos:0"] = "Backup"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDocumentAnalysis(configuration);

        if (registerDriveClient)
        {
            var fake = new FakeDriveClient(
                new Dictionary<string, IReadOnlyList<DriveItem>>());
            services.AddSingleton<IDriveClient>(fake);

            if (registerFakeMcp)
            {
                services.RemoveAll<IDriveMcpClient>();
                services.AddSingleton<IDriveMcpClient>(sp =>
                {
                    var opts = sp.GetRequiredService<IOptions<DocumentAnalysisOptions>>();
                    return new FakeDriveMcpClient(opts, sp.GetRequiredService<IDriveClient>());
                });
            }
        }

        return services.BuildServiceProvider();
    }
}
