using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis;
using ISOAuditAgent.DocumentAnalysis.Drive;

namespace ISOAuditAgent.API.Internal;

/// <summary>
/// Endpoint <b>solo Development</b> que ejecuta
/// <see cref="DocumentAnalysisRunner.ExecuteAsync"/> end-to-end y
/// devuelve el <see cref="DocumentosExtraidos"/> resultante.
/// </summary>
/// <remarks>
/// <para>
/// Cumple el criterio "endpoint interno opcional para demos" de la
/// Fase 5 (§11). No es parte del contrato productivo: el runner se
/// invocará desde el host MAF en Fase 9 (programáticamente, no por
/// HTTP).
/// </para>
/// <para>
/// Por defecto trunca <see cref="DocumentoExtraido.ContenidoTextual"/>
/// a 500 caracteres para que la respuesta sea browseable. Pasar
/// <c>?full=true</c> para conservar el texto completo (útil para
/// validar serialización contra el contrato del orquestador).
/// </para>
/// </remarks>
internal static class DocumentAnalysisRunEndpoint
{
    private const int PreviewMaxChars = 500;

    public static IEndpointRouteBuilder MapDocumentAnalysisRun(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal/document-analysis")
            .WithTags("Internal — DocumentAnalysisRunner (dev-only)");

        group.MapPost("/run", HandleAsync)
            .WithName("DocumentAnalysisRun");

        return app;
    }

    private static async Task<IResult> HandleAsync(
        DocumentAnalysisRunRequest body,
        bool? full,
        DocumentAnalysisRunner runner,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Results.BadRequest(new
            {
                title = "Body requerido",
                detail = "Enviar un JSON con auditoriaId y proyectoId."
            });
        }

        if (body.AuditoriaId <= 0 || body.ProyectoId <= 0)
        {
            return Results.BadRequest(new
            {
                title = "AuditoriaId y ProyectoId deben ser > 0",
                detail = $"Recibido: AuditoriaId={body.AuditoriaId}, ProyectoId={body.ProyectoId}."
            });
        }

        var includeFullText = full ?? false;

        try
        {
            var request = new DocumentAnalysisRequest(
                AuditoriaId: body.AuditoriaId,
                ProyectoId: body.ProyectoId,
                SolicitudEnUtc: DateTimeOffset.UtcNow);

            var result = await runner
                .ExecuteAsync(request, cancellationToken)
                .ConfigureAwait(false);

            var documentos = result.Documentos
                .Select(d => Project(d, includeFullText))
                .ToList();

            return Results.Ok(new DocumentAnalysisRunResponse(
                AuditoriaId: result.AuditoriaId,
                ProyectoId: result.ProyectoId,
                TotalDocumentos: documentos.Count,
                IncluyeTextoCompleto: includeFullText,
                Documentos: documentos));
        }
        catch (ProyectoDriveMappingNotFoundException ex)
        {
            return Results.Problem(
                title: "ProyectoId sin mapeo de Drive",
                detail: ex.Message,
                statusCode: StatusCodes.Status404NotFound);
        }
        catch (OperationCanceledException)
        {
            return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error ejecutando DocumentAnalysisRunner para " +
                "AuditoriaId={AuditoriaId}, ProyectoId={ProyectoId}.",
                body.AuditoriaId, body.ProyectoId);

            return Results.Problem(
                title: "Error invocando DocumentAnalysisRunner",
                detail: $"{ex.GetType().Name}: {ex.Message}" +
                        (ex.InnerException is not null
                            ? $" — causa: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}"
                            : string.Empty),
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static DocumentoProjection Project(DocumentoExtraido d, bool includeFullText)
    {
        var texto = d.ContenidoTextual;
        var preview = texto.Length <= PreviewMaxChars
            ? texto
            : texto[..PreviewMaxChars] + "…";

        return new DocumentoProjection(
            IdEnFuente: d.IdEnFuente,
            NombreArchivo: d.NombreArchivo,
            Fuente: d.Fuente.ToString(),
            UrlReferencia: d.UrlReferencia,
            FormatoContenido: d.FormatoContenido.ToString(),
            HashContenido: d.HashContenido,
            CaracteresTexto: texto.Length,
            TextoPreview: preview,
            TextoCompleto: includeFullText ? texto : null,
            Metadatos: d.Metadatos);
    }

    public sealed record DocumentAnalysisRunRequest(
        int AuditoriaId,
        int ProyectoId);

    private sealed record DocumentAnalysisRunResponse(
        int AuditoriaId,
        int ProyectoId,
        int TotalDocumentos,
        bool IncluyeTextoCompleto,
        IReadOnlyList<DocumentoProjection> Documentos);

    private sealed record DocumentoProjection(
        string IdEnFuente,
        string NombreArchivo,
        string Fuente,
        string? UrlReferencia,
        string FormatoContenido,
        string HashContenido,
        int CaracteresTexto,
        string TextoPreview,
        string? TextoCompleto,
        DocumentoMetadatos Metadatos);
}
