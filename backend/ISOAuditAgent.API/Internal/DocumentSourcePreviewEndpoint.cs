using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Sources;

namespace ISOAuditAgent.API.Internal;

/// <summary>
/// Endpoint <b>solo Development</b> para inspeccionar el pipeline
/// de DocumentAnalysis sin necesitar el <c>Runner</c> (Fase 5):
/// <c>resolver → listing → descarga → stream → hash</c> de Fase 3 y,
/// opcionalmente, <c>parser → texto + formato</c> de Fase 4.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description>
///     <c>GET /internal/document-source/{proyectoId}</c> — Fase 3: hash
///     SHA-256 + metadatos (sin parsear).
///   </description></item>
///   <item><description>
///     <c>GET /internal/document-source/{proyectoId}?parse=true</c> —
///     además ejecuta el parser apropiado (PDF/DOCX/XLSX) y devuelve
///     <c>formato</c>, <c>caracteresExtraidos</c> y <c>textoPreview</c>
///     (primeros 500 caracteres).
///   </description></item>
///   <item><description>
///     <c>?parse=true&amp;full=true</c> — además devuelve
///     <c>textoCompleto</c>. Útil para validar UTF-8/NFC y la salida
///     Markdown de Excel; cuidado con archivos grandes.
///   </description></item>
/// </list>
/// Apto para inspección rápida desde curl / Postman / browser; no apto
/// para producción.
/// </remarks>
internal static class DocumentSourcePreviewEndpoint
{
    private const int PreviewMaxChars = 500;

    public static IEndpointRouteBuilder MapDocumentSourcePreview(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal/document-source")
            .WithTags("Internal — DocumentSource preview (dev-only)");

        group.MapGet("/{proyectoId:int}", HandleAsync)
            .WithName("DocumentSourcePreview");

        return app;
    }

    private static async Task<IResult> HandleAsync(
        int proyectoId,
        bool? parse,
        bool? full,
        IDocumentSource source,
        DocumentParserRegistry registry,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var doParse = parse ?? false;
        var includeFullText = doParse && (full ?? false);

        try
        {
            var items = new List<DocumentSourcePreviewItem>();
            var totalBytes = 0L;

            await foreach (var raw in source
                .EnumerateAsync(proyectoId, cancellationToken)
                .ConfigureAwait(false))
            {
                using (raw)
                {
                    var sha256 = await ContentHasher
                        .ComputeSha256OfStreamAsync(raw.Contenido, cancellationToken)
                        .ConfigureAwait(false);

                    var bytesLeidos = raw.Contenido.Length;
                    totalBytes += bytesLeidos;

                    var parseInfo = doParse
                        ? await TryParseAsync(raw, registry, includeFullText, logger, cancellationToken)
                            .ConfigureAwait(false)
                        : null;

                    items.Add(new DocumentSourcePreviewItem(
                        IdEnFuente: raw.IdEnFuente,
                        NombreArchivo: raw.NombreArchivo,
                        MimeType: raw.MimeType,
                        TamanoMetadato: raw.Tamano,
                        BytesLeidos: bytesLeidos,
                        Sha256: sha256,
                        UrlReferencia: raw.UrlReferencia,
                        FechaCreacion: raw.FechaCreacion,
                        FechaModificacion: raw.FechaModificacion,
                        Parse: parseInfo));
                }
            }

            logger.LogInformation(
                "DocumentSourcePreview: ProyectoId={ProyectoId} → {Total} docs, {Bytes} bytes, parse={Parse}.",
                proyectoId, items.Count, totalBytes, doParse);

            return Results.Ok(new DocumentSourcePreviewResponse(
                ProyectoId: proyectoId,
                Fuente: source.Fuente.ToString(),
                TotalDocumentos: items.Count,
                TotalBytes: totalBytes,
                Parseo: doParse,
                Documentos: items));
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
                "Error en DocumentSourcePreview para ProyectoId={ProyectoId}.",
                proyectoId);

            return Results.Problem(
                title: "Error invocando IDocumentSource",
                detail: $"{ex.GetType().Name}: {ex.Message}" +
                        (ex.InnerException is not null
                            ? $" — causa: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}"
                            : string.Empty),
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<DocumentParsePreview> TryParseAsync(
        RawDocument raw,
        DocumentParserRegistry registry,
        bool includeFullText,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!registry.TryGetParser(raw.MimeType, out var parser))
        {
            return new DocumentParsePreview(
                ParserDisponible: false,
                Formato: null,
                CaracteresExtraidos: 0,
                TextoPreview: null,
                TextoCompleto: null,
                Error: $"No hay parser registrado para MIME '{raw.MimeType}'.");
        }

        try
        {
            if (raw.Contenido.CanSeek) raw.Contenido.Position = 0;

            var result = await parser
                .ParseAsync(raw.Contenido, raw.NombreArchivo, cancellationToken)
                .ConfigureAwait(false);

            var texto = result.TextoNormalizado;
            var preview = texto.Length <= PreviewMaxChars
                ? texto
                : texto[..PreviewMaxChars] + "…";

            return new DocumentParsePreview(
                ParserDisponible: true,
                Formato: result.Formato.ToString(),
                CaracteresExtraidos: texto.Length,
                TextoPreview: preview,
                TextoCompleto: includeFullText ? texto : null,
                Error: null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex,
                "Parser falló para {Archivo} ({Mime}).",
                raw.NombreArchivo, raw.MimeType);

            return new DocumentParsePreview(
                ParserDisponible: true,
                Formato: null,
                CaracteresExtraidos: 0,
                TextoPreview: null,
                TextoCompleto: null,
                Error: $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private sealed record DocumentSourcePreviewResponse(
        int ProyectoId,
        string Fuente,
        int TotalDocumentos,
        long TotalBytes,
        bool Parseo,
        IReadOnlyList<DocumentSourcePreviewItem> Documentos);

    private sealed record DocumentSourcePreviewItem(
        string IdEnFuente,
        string NombreArchivo,
        string MimeType,
        long? TamanoMetadato,
        long BytesLeidos,
        string Sha256,
        string? UrlReferencia,
        DateTimeOffset? FechaCreacion,
        DateTimeOffset? FechaModificacion,
        DocumentParsePreview? Parse);

    private sealed record DocumentParsePreview(
        bool ParserDisponible,
        string? Formato,
        int CaracteresExtraidos,
        string? TextoPreview,
        string? TextoCompleto,
        string? Error);
}
