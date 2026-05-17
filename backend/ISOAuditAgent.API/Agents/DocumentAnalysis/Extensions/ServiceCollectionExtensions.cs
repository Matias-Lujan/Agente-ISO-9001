using System.ClientModel;
using ISOAuditAgent.DocumentAnalysis.Agent;
using ISOAuditAgent.DocumentAnalysis.Agent.Configuration;
using ISOAuditAgent.DocumentAnalysis.Agent.Tools;
using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Mcp;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Sources;
using ISOAuditAgent.DocumentAnalysis.Tailoring;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

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
    ///     <see cref="McpDriveClientOptions"/> bindeadas desde la sección
    ///     <c>"McpDrive"</c> (URL del endpoint MCP local, p. ej.
    ///     <c>http://localhost:5180/mcp/drive</c>).
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IDriveMcpClient"/> → <see cref="DriveMcpSdkClient"/>:
    ///     consumidor HTTP Streamable de las tools
    ///     <c>list_project_files</c> / <c>get_file_content</c> / por URL.
    ///     El <see cref="IDriveClient"/> real solo se registra en el host
    ///     para <c>DriveMcpTools</c> (<c>AddGoogleDriveMcpServer</c> en el
    ///     host), no
    ///     para <see cref="DriveDocumentSource"/> ni <see cref="TailoringReader"/>.
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IDocumentSource"/> →
    ///     <see cref="DriveDocumentSource"/>. Se registra como servicio
    ///     en la colección <c>IEnumerable&lt;IDocumentSource&gt;</c> para
    ///     poder convivir con futuras fuentes. Requiere
    ///     <see cref="IDriveMcpClient"/> (y configuración <c>McpDrive</c>).
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IDocumentParser"/> (PDF / DOCX / XLSX) +
    ///     <see cref="DocumentParserRegistry"/>. Despacho por MIME.
    ///     <see cref="ContentHasher"/> es estático y no requiere registro.
    ///   </description></item>
    /// </list>
    /// <para>
    /// El agente MAF que orquesta todo el pipeline (Fase F del plan de
    /// desarrollo) se registra desde su propia extensión cuando se
    /// implemente; este método solo cubre las piezas determinísticas
    /// reutilizables (sources, parsers, hashing).
    /// </para>
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

        services
            .AddOptions<McpDriveClientOptions>()
            .Bind(configuration.GetSection(McpDriveClientOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddSingleton<IDriveMcpClient, DriveMcpSdkClient>();

        services.AddSingleton<IProyectoDriveResolver, OptionsBackedProyectoDriveResolver>();
        services.AddSingleton<DriveExclusionPolicy>();
        services.AddSingleton<IDriveListingService, DriveListingService>();

        services.AddSingleton<IDocumentSource, DriveDocumentSource>();

        services.AddSingleton<IDocumentParser, PdfDocumentParser>();
        services.AddSingleton<IDocumentParser, DocxDocumentParser>();
        services.AddSingleton<IDocumentParser, XlsxDocumentParser>();
        services.AddSingleton<DocumentParserRegistry>();

        services.AddSingleton<XlsxTableExtractor>();
        services.AddSingleton<ITailoringReader, TailoringReader>();

        // Fase F: agente DocumentAnalysis (Gemini OpenAI-compatible + tools locales)
        services
            .AddOptions<DocumentAnalysisAgentOptions>()
            .Bind(configuration.GetSection(DocumentAnalysisAgentOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddScoped<TailoringTool>();
        services.AddScoped<VerificarArtefactoTool>();
        services.AddScoped<IDocumentAnalysisAgent, DocumentAnalysisAgent>();

        services.AddSingleton<IChatClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<DocumentAnalysisAgentOptions>>().Value;
            var apiKey = Environment.GetEnvironmentVariable(opts.Llm.ApiKeyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    $"Variable de entorno '{opts.Llm.ApiKeyEnvironmentVariable}' no configurada. " +
                    "Definila para usar el cliente LLM de DocumentAnalysisAgent.");
            }

            var oaiOpts = new OpenAIClientOptions { Endpoint = new Uri(opts.Llm.BaseUrl) };
            var client = new OpenAIClient(new ApiKeyCredential(apiKey), oaiOpts);
            return client.GetChatClient(opts.Llm.Model).AsIChatClient();
        });

        return services;
    }
}
