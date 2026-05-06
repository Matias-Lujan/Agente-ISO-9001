using System.Diagnostics;
using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Runner;
using ISOAuditAgent.DocumentAnalysis.Sources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ISOAuditAgent.DocumentAnalysis;

/// <summary>
/// Punto de entrada del agente DocumentAnalysis.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ExecuteAsync"/> ejecuta el pipeline síncrono completo
/// <c>fuente → descarga → hash → parse → DocumentoExtraido</c> sobre
/// todas las <see cref="IDocumentSource"/> registradas para el
/// <see cref="DocumentAnalysisRequest.ProyectoId"/> y arma el
/// <see cref="DocumentosExtraidos"/> que consume el orquestador.
/// </para>
/// <para>
/// <b>Política de errores (Fase 5)</b>: los errores por documento
/// individual <i>no</i> abortan el lote. Se registra un log y el
/// documento se omite del resultado:
/// </para>
/// <list type="bullet">
///   <item><description>
///     MIME sin parser registrado → <see cref="LogLevel.Warning"/>,
///     omitido.
///   </description></item>
///   <item><description>
///     Excepción del parser o del cálculo de hash →
///     <see cref="LogLevel.Error"/>, omitido.
///   </description></item>
///   <item><description>
///     <see cref="OperationCanceledException"/> sí se propaga: la
///     cancelación cooperativa interrumpe el lote completo.
///   </description></item>
/// </list>
/// <para>
/// Los errores de la propia fuente (por ejemplo, <c>FolderId</c> sin
/// mapeo, fallo de credenciales de Drive) sí se propagan: no son
/// "fallas por documento", son fallas de configuración del lote.
/// </para>
/// </remarks>
public sealed class DocumentAnalysisRunner
{
    private readonly IEnumerable<IDocumentSource> _sources;
    private readonly DocumentParserRegistry _parsers;
    private readonly ILogger<DocumentAnalysisRunner> _logger;

    public DocumentAnalysisRunner(
        IEnumerable<IDocumentSource> sources,
        DocumentParserRegistry parsers,
        ILogger<DocumentAnalysisRunner>? logger = null)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
        _logger = logger ?? NullLogger<DocumentAnalysisRunner>.Instance;
    }

    public async Task<DocumentosExtraidos> ExecuteAsync(
        DocumentAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();
        var documentos = new List<DocumentoExtraido>();
        var stats = new RunStats();

        var sources = _sources as IReadOnlyCollection<IDocumentSource>
                     ?? _sources.ToArray();

        _logger.LogInformation(
            "DocumentAnalysisRunner: iniciando AuditoriaId={AuditoriaId}, " +
            "ProyectoId={ProyectoId}, fuentes={Sources}.",
            request.AuditoriaId, request.ProyectoId, sources.Count);

        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessSourceAsync(
                source, request, documentos, stats, cancellationToken)
                .ConfigureAwait(false);
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "DocumentAnalysisRunner: AuditoriaId={AuditoriaId} terminado " +
            "(procesados={Procesados}, ok={Ok}, sinParser={SinParser}, " +
            "fallidos={Fallidos}, elapsed={ElapsedMs}ms).",
            request.AuditoriaId, stats.Procesados, documentos.Count,
            stats.OmitidosPorParserAusente, stats.Fallidos,
            stopwatch.ElapsedMilliseconds);

        return new DocumentosExtraidos(
            AuditoriaId: request.AuditoriaId,
            ProyectoId: request.ProyectoId,
            Documentos: documentos);
    }

    private async Task ProcessSourceAsync(
        IDocumentSource source,
        DocumentAnalysisRequest request,
        List<DocumentoExtraido> destino,
        RunStats stats,
        CancellationToken cancellationToken)
    {
        await foreach (var raw in source
            .EnumerateAsync(request.ProyectoId, cancellationToken)
            .ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (raw)
            {
                stats.Procesados++;

                if (!_parsers.TryGetParser(raw.MimeType, out var parser))
                {
                    _logger.LogWarning(
                        "DocumentAnalysisRunner: documento omitido (sin parser) " +
                        "Archivo={Archivo}, Mime={Mime}, IdEnFuente={Id}.",
                        raw.NombreArchivo, raw.MimeType, raw.IdEnFuente);
                    stats.OmitidosPorParserAusente++;
                    continue;
                }

                DocumentoExtraido extraido;
                try
                {
                    extraido = await BuildDocumentoExtraidoAsync(
                        source.Fuente, raw, parser, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "DocumentAnalysisRunner: documento omitido (falló parse/hash) " +
                        "Archivo={Archivo}, Mime={Mime}, IdEnFuente={Id}.",
                        raw.NombreArchivo, raw.MimeType, raw.IdEnFuente);
                    stats.Fallidos++;
                    continue;
                }

                destino.Add(extraido);
            }
        }
    }

    private static async Task<DocumentoExtraido> BuildDocumentoExtraidoAsync(
        FuenteDocumento fuente,
        RawDocument raw,
        IDocumentParser parser,
        CancellationToken cancellationToken)
    {
        // Orden importante: si la fuente es binaria primero hashemos los
        // bytes (el helper rebobina el stream a Position=0 al terminar);
        // luego el parser lee desde 0. Si la fuente NO es binaria,
        // necesitamos el texto antes de hashear: en ese caso parseamos
        // primero y diferimos el hash.
        string? hash = null;

        if (DocumentHashStrategy.TieneArtefactoBinario(fuente))
        {
            hash = await ContentHasher
                .ComputeSha256OfStreamAsync(raw.Contenido, cancellationToken)
                .ConfigureAwait(false);
        }

        var parseResult = await parser
            .ParseAsync(raw.Contenido, raw.NombreArchivo, cancellationToken)
            .ConfigureAwait(false);

        hash ??= ContentHasher.ComputeSha256OfText(parseResult.TextoNormalizado);

        return new DocumentoExtraido(
            IdEnFuente: raw.IdEnFuente,
            NombreArchivo: raw.NombreArchivo,
            Fuente: fuente,
            UrlReferencia: raw.UrlReferencia,
            ContenidoTextual: parseResult.TextoNormalizado,
            FormatoContenido: parseResult.Formato,
            HashContenido: hash,
            Metadatos: new DocumentoMetadatos(
                Autor: null,
                FechaCreacion: raw.FechaCreacion,
                FechaModificacion: raw.FechaModificacion,
                Version: null,
                Estado: null));
    }

    private sealed class RunStats
    {
        public int Procesados;
        public int OmitidosPorParserAusente;
        public int Fallidos;
    }
}
