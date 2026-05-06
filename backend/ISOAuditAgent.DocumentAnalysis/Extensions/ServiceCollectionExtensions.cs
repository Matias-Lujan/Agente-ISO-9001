using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Sources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Extensions;

/// <summary>
/// Registro del módulo DocumentAnalysis en el contenedor de DI del host.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra los servicios del módulo DocumentAnalysis:
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="DocumentAnalysisOptions"/> bindeadas desde la sección
    ///     <c>"DocumentAnalysis"</c> de <paramref name="configuration"/>,
    ///     con validación por <c>DataAnnotations</c> y validación estructural
    ///     adicional (ProyectoIds únicos, regex compilables).
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IProyectoDriveResolver"/> →
    ///     <see cref="OptionsBackedProyectoDriveResolver"/>.
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IDocumentSource"/> →
    ///     <see cref="DriveDocumentSource"/> (Fase 3). Se registra como
    ///     servicio nombrado en la colección <c>IEnumerable&lt;IDocumentSource&gt;</c>
    ///     para que en el futuro convivan múltiples fuentes (Trello, Clockify).
    ///     Requiere que el host registre <see cref="IDriveClient"/>.
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IDocumentParser"/> (PDF / DOCX / XLSX) +
    ///     <see cref="DocumentParserRegistry"/> (Fase 4). Despacho por MIME.
    ///     <see cref="ContentHasher"/> es estático y no requiere registro.
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="DocumentAnalysisRunner"/> (stub hasta Fase 5).
    ///   </description></item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddDocumentAnalysis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DocumentAnalysisOptions>()
            .Bind(configuration.GetSection(DocumentAnalysisOptions.SectionName))
            .ValidateDataAnnotations();

        services
            .AddSingleton<IValidateOptions<DocumentAnalysisOptions>,
                          DocumentAnalysisOptionsValidator>();

        services.AddSingleton<IProyectoDriveResolver, OptionsBackedProyectoDriveResolver>();
        services.AddSingleton<DriveExclusionPolicy>();
        services.AddSingleton<IDriveListingService, DriveListingService>();

        services.AddSingleton<IDocumentSource, DriveDocumentSource>();

        services.AddSingleton<IDocumentParser, PdfDocumentParser>();
        services.AddSingleton<IDocumentParser, DocxDocumentParser>();
        services.AddSingleton<IDocumentParser, XlsxDocumentParser>();
        services.AddSingleton<DocumentParserRegistry>();

        services.AddSingleton<DocumentAnalysisRunner>();

        return services;
    }
}
