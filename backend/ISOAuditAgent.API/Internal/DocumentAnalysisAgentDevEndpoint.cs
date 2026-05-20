using ISOAuditAgent.DocumentAnalysis;
using ISOAuditAgent.DocumentAnalysis.Agent;
using ISOAuditAgent.DocumentAnalysis.Agent.Validation;
using ISOAuditAgent.DocumentAnalysis.Tailoring;

namespace ISOAuditAgent.API.Internal;

/// <summary>
/// Endpoint <b>solo Development</b> (smoke E2E del §14.14 del handoff Fase F):
/// <c>POST /internal/document-analysis/agent/run</c> invoca
/// <see cref="IDocumentAnalysisAgent.ExecuteAsync"/> y devuelve
/// <c>DocumentosExtraidos</c> (contrato) serializado como JSON.
/// </summary>
internal static class DocumentAnalysisAgentDevEndpoint
{
    public static IEndpointRouteBuilder MapDocumentAnalysisAgentDev(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal/document-analysis")
            .WithTags("Internal — DocumentAnalysis agent (dev-only)");

        group.MapPost("/agent/run", HandleRunAsync)
            .WithName("DocumentAnalysisAgentRun");

        return app;
    }

    private static async Task<IResult> HandleRunAsync(
        DocumentAnalysisAgentRunBody? body,
        IDocumentAnalysisAgent agent,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var b = body ?? new DocumentAnalysisAgentRunBody();
        var request = new DocumentAnalysisRequest(
            b.AuditoriaId,
            b.ProyectoId,
            b.EtapaId,
            b.SolicitudEnUtc ?? DateTimeOffset.UtcNow);

        try
        {
            var result = await agent
                .ExecuteAsync(request, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "DocumentAnalysisAgentRun: OK AuditoriaId={AuditoriaId} ProyectoId={ProyectoId} EtapaId={EtapaId} artefactos={Count}.",
                request.AuditoriaId, request.ProyectoId, request.EtapaId,
                result.Artefactos.Count);

            return Results.Ok(result);
        }
        catch (TailoringWorkbookNotFoundException ex)
        {
            return Results.Problem(
                title: "Tailoring FR 29 no disponible",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvariantViolationException ex)
        {
            logger.LogWarning(ex, "DocumentAnalysisAgentRun: invariantes no cumplidas tras reintentos.");
            return Results.Problem(
                title: "Salida del agente inválida (invariantes)",
                detail: ex.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("Variable de entorno", StringComparison.Ordinal))
        {
            return Results.Problem(
                title: "Configuración LLM incompleta",
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "DocumentAnalysisAgentRun: error AuditoriaId={AuditoriaId} ProyectoId={ProyectoId} EtapaId={EtapaId}.",
                request.AuditoriaId, request.ProyectoId, request.EtapaId);

            return Results.Problem(
                title: "Error ejecutando DocumentAnalysisAgent",
                detail: $"{ex.GetType().Name}: {ex.Message}",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>Cuerpo JSON opcional; valores por defecto alineados al seed mínimo (1,1,1).</summary>
    public sealed class DocumentAnalysisAgentRunBody
    {
        public int AuditoriaId { get; set; } = 1;

        public int ProyectoId { get; set; } = 1;

        public int EtapaId { get; set; } = 1;

        public DateTimeOffset? SolicitudEnUtc { get; set; }
    }
}
