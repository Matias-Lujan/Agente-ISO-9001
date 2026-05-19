using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis;
using ISOAuditAgent.DocumentAnalysis.Agent;
using ISOAuditAgent.DocumentAnalysis.Agent.Configuration;
using ISOAuditAgent.DocumentAnalysis.Agent.Models;
using ISOAuditAgent.DocumentAnalysis.Agent.Tools;
using ISOAuditAgent.DocumentAnalysis.Configuration;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Tailoring;
using ISOAuditAgent.DocumentAnalysis.Tests.Drive;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Agent;

public sealed class DocumentAnalysisAgentTests
{
    [Fact]
    public async Task ExecuteAsync_SinDeclarar_en_NoBuscado()
    {
        var view = new ArtefactoEsperadoView(
            42, "FR 29", "Tailoring", 1, 1, "Planificación",
            ExigibilidadArtefacto.Exigible, ObligatoriedadArtefacto.Mandatorio, "FR-29.xlsx");
        var contexto = new ProyectoContexto(
            1, 1, "PR 11-13", 1, 1, "Planificación", TipoProyecto.A,
            "fold-p", "fold-t", new[] { view });

        var llmJson = """
                     {"artefactos":[{"artefactoEsperadoId":42,"estadoTailoring":"SinDeclararEnTailoring","justificacionNoAplica":null,"urlReferenciaTailoring":null}]}
                     """;

        var agent = CreateAgent(contexto, llmJson);
        var result = await agent.ExecuteAsync(
            new DocumentAnalysisRequest(9, 1, 1, DateTimeOffset.UtcNow));

        Assert.Equal(9, result.AuditoriaId);
        var a = Assert.Single(result.Artefactos);
        Assert.Equal(EstadoTailoring.SinDeclararEnTailoring, a.EstadoTailoring);
        Assert.Equal(EstadoDisponibilidad.NoBuscado, a.EstadoDisponibilidad);
        Assert.Null(a.DocumentoEncontrado);
    }

    [Fact]
    public async Task ExecuteAsync_reintenta_si_JSON_invalido()
    {
        var view = new ArtefactoEsperadoView(
            42, "FR 29", "Tailoring", 1, 1, "Planificación",
            ExigibilidadArtefacto.Exigible, ObligatoriedadArtefacto.Mandatorio, "FR-29.xlsx");
        var contexto = new ProyectoContexto(
            1, 1, "PR", 1, 1, "Planificación", TipoProyecto.A,
            null, null, new[] { view });

        var ok = """
                 {"artefactos":[{"artefactoEsperadoId":42,"estadoTailoring":"SinDeclararEnTailoring","justificacionNoAplica":null,"urlReferenciaTailoring":null}]}
                 """;

        var chat = new FixedJsonChatClient("not-json", ok);
        var agent = CreateAgent(contexto, chat, maxRetries: 1);

        var result = await agent.ExecuteAsync(new DocumentAnalysisRequest(1, 1, 1, DateTimeOffset.UtcNow));
        Assert.Single(result.Artefactos);
    }

    private static DocumentAnalysisAgent CreateAgent(
        ProyectoContexto contexto,
        string llmJson,
        int maxRetries = 0) =>
        CreateAgent(contexto, new FixedJsonChatClient(llmJson), maxRetries);

    private static DocumentAnalysisAgent CreateAgent(
        ProyectoContexto contexto,
        FixedJsonChatClient chat,
        int maxRetries = 0)
    {
        var fakeDrive = new FakeDriveClient(
            new Dictionary<string, IReadOnlyList<DriveItem>> { ["root"] = [] });
        var docOpts = Options.Create(new DocumentAnalysisOptions
        {
            Drive = new DriveOptions
            {
                Mappings = [new ProyectoFolderMapping { ProyectoId = 1, FolderId = "root" }],
                MimeTypes = ["application/pdf"],
                Exclusiones = new DriveExclusionOptions()
            }
        });
        var mcp = new FakeDriveMcpClient(docOpts, fakeDrive);
        var parsers = new DocumentParserRegistry(
            [new PdfDocumentParser(), new DocxDocumentParser(), new XlsxDocumentParser()]);
        var verificar = new VerificarArtefactoTool(mcp, parsers);
        var tailoring = new TailoringTool(new EmptyTailoringReader());
        var agentOpts = Options.Create(new DocumentAnalysisAgentOptions
        {
            MaxLlmRetries = maxRetries,
            TemperatureLow = 0.1f,
            Llm = new LlmClientOptions
            {
                BaseUrl = "https://example.com/",
                Model = "test",
                ApiKeyEnvironmentVariable = "NONE"
            }
        });

        return new DocumentAnalysisAgent(
            new StubContexto(contexto),
            tailoring,
            verificar,
            chat,
            agentOpts,
            NullLogger<DocumentAnalysisAgent>.Instance);
    }

    private sealed class StubContexto(ProyectoContexto ctx) : IContextoAuditoriaService
    {
        public Task<ProyectoContexto> GetContextoAuditoriaAsync(
            int proyectoId, int etapaIdActual, CancellationToken cancellationToken = default)
            => Task.FromResult(ctx);
    }

    private sealed class EmptyTailoringReader : ITailoringReader
    {
        public Task<IReadOnlyList<EntradaTailoring>> ReadAsync(
            int proyectoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EntradaTailoring>>([]);
    }
}
