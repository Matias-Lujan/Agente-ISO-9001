using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using ISOAuditAgent.DocumentAnalysis.Sources;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Runner;

public sealed class DocumentAnalysisRunnerTests
{
    private const string PdfMime = "application/pdf";
    private const string DocxMime =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    [Fact]
    public async Task ExecuteAsync_propaga_AuditoriaId_y_ProyectoId_al_resultado()
    {
        var runner = BuildRunner(
            new FakeDocumentSource(
                FuenteDocumento.GoogleDrive,
                Enumerable.Empty<RawDocumentSpec>()),
            new FakeDocumentParser(PdfMime));

        var request = new DocumentAnalysisRequest(
            AuditoriaId: 42, ProyectoId: 7,
            SolicitudEnUtc: DateTimeOffset.UtcNow);

        var result = await runner.ExecuteAsync(request);

        Assert.Equal(42, result.AuditoriaId);
        Assert.Equal(7, result.ProyectoId);
        Assert.Empty(result.Documentos);
    }

    [Fact]
    public async Task ExecuteAsync_invoca_la_fuente_con_el_ProyectoId_del_request()
    {
        int? proyectoIdRecibido = null;
        var source = new FakeDocumentSource(
            FuenteDocumento.GoogleDrive,
            Enumerable.Empty<RawDocumentSpec>(),
            onProyectoIdRecibido: pid => proyectoIdRecibido = pid);

        var runner = BuildRunner(source, new FakeDocumentParser(PdfMime));

        await runner.ExecuteAsync(new DocumentAnalysisRequest(1, 99, DateTimeOffset.UtcNow));

        Assert.Equal(99, proyectoIdRecibido);
    }

    [Fact]
    public async Task ExecuteAsync_arma_DocumentoExtraido_con_metadata_y_hash_de_bytes_para_Drive()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("hola mundo");
        var hashEsperado = ContentHasher.ComputeSha256OfBytes(bytes);
        var fechaCre = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var fechaMod = new DateTimeOffset(2025, 6, 7, 8, 9, 10, TimeSpan.Zero);

        var source = new FakeDocumentSource(
            FuenteDocumento.GoogleDrive,
            new[]
            {
                new RawDocumentSpec(
                    IdEnFuente: "drive-1",
                    NombreArchivo: "manual.pdf",
                    MimeType: PdfMime,
                    Tamano: bytes.Length,
                    FechaCreacion: fechaCre,
                    FechaModificacion: fechaMod,
                    UrlReferencia: "https://drive.google.com/x",
                    StreamFactory: () => new MemoryStream(bytes, writable: false))
            });

        var parser = new FakeDocumentParser(
            PdfMime,
            (_, archivo) => new DocumentParseResult(
                $"texto-de-{archivo}", FormatoContenido.PlainText));

        var runner = BuildRunner(source, parser);
        var request = new DocumentAnalysisRequest(1, 1, DateTimeOffset.UtcNow);

        var result = await runner.ExecuteAsync(request);

        var doc = Assert.Single(result.Documentos);
        Assert.Equal("drive-1", doc.IdEnFuente);
        Assert.Equal("manual.pdf", doc.NombreArchivo);
        Assert.Equal(FuenteDocumento.GoogleDrive, doc.Fuente);
        Assert.Equal("https://drive.google.com/x", doc.UrlReferencia);
        Assert.Equal("texto-de-manual.pdf", doc.ContenidoTextual);
        Assert.Equal(FormatoContenido.PlainText, doc.FormatoContenido);
        Assert.Equal(hashEsperado, doc.HashContenido);
        Assert.Equal(fechaCre, doc.Metadatos.FechaCreacion);
        Assert.Equal(fechaMod, doc.Metadatos.FechaModificacion);
        Assert.Null(doc.Metadatos.Autor);
        Assert.Null(doc.Metadatos.Version);
        Assert.Null(doc.Metadatos.Estado);
    }

    [Fact]
    public async Task ExecuteAsync_para_fuente_no_binaria_hashea_sobre_texto_normalizado()
    {
        // Trello: hash sobre texto canónico, no sobre bytes.
        var texto = "tarjeta normalizada";
        var hashEsperado = ContentHasher.ComputeSha256OfText(texto);

        var source = new FakeDocumentSource(
            FuenteDocumento.Trello,
            new[]
            {
                new RawDocumentSpec(
                    IdEnFuente: "card-1",
                    NombreArchivo: "tarjeta",
                    MimeType: "text/plain",
                    Tamano: null,
                    FechaCreacion: null,
                    FechaModificacion: null,
                    UrlReferencia: null,
                    StreamFactory: () => new MemoryStream(new byte[] { 0xFF }))
            });

        var parser = new FakeDocumentParser(
            "text/plain",
            (_, _) => new DocumentParseResult(texto, FormatoContenido.PlainText));

        var runner = BuildRunner(source, parser);

        var result = await runner.ExecuteAsync(
            new DocumentAnalysisRequest(1, 1, DateTimeOffset.UtcNow));

        var doc = Assert.Single(result.Documentos);
        Assert.Equal(FuenteDocumento.Trello, doc.Fuente);
        Assert.Equal(hashEsperado, doc.HashContenido);
    }

    [Fact]
    public async Task ExecuteAsync_omite_documento_con_MIME_sin_parser_pero_continua_el_lote()
    {
        var source = new FakeDocumentSource(
            FuenteDocumento.GoogleDrive,
            new[]
            {
                Spec("doc-1", "raro.xyz", "application/x-misterio"),
                Spec("doc-2", "valido.pdf", PdfMime)
            });

        var parser = new FakeDocumentParser(PdfMime);
        var runner = BuildRunner(source, parser);

        var result = await runner.ExecuteAsync(
            new DocumentAnalysisRequest(1, 1, DateTimeOffset.UtcNow));

        var doc = Assert.Single(result.Documentos);
        Assert.Equal("doc-2", doc.IdEnFuente);
    }

    [Fact]
    public async Task ExecuteAsync_omite_documento_que_falla_pero_emite_los_demas()
    {
        var source = new FakeDocumentSource(
            FuenteDocumento.GoogleDrive,
            new[]
            {
                Spec("doc-1", "ok-1.pdf", PdfMime),
                Spec("doc-2", "boom.pdf", PdfMime),
                Spec("doc-3", "ok-3.pdf", PdfMime)
            });

        // Parser que falla solo cuando el archivo es "boom.pdf".
        var parser = new FakeDocumentParser(
            PdfMime,
            (_, archivo) =>
            {
                if (archivo == "boom.pdf")
                {
                    throw new InvalidOperationException("DOCX/PDF corrupto simulado.");
                }
                return new DocumentParseResult($"texto-{archivo}", FormatoContenido.PlainText);
            });

        var runner = BuildRunner(source, parser);

        var result = await runner.ExecuteAsync(
            new DocumentAnalysisRequest(1, 1, DateTimeOffset.UtcNow));

        Assert.Equal(2, result.Documentos.Count);
        Assert.DoesNotContain(result.Documentos, d => d.IdEnFuente == "doc-2");
        Assert.Contains(result.Documentos, d => d.IdEnFuente == "doc-1");
        Assert.Contains(result.Documentos, d => d.IdEnFuente == "doc-3");
    }

    [Fact]
    public async Task ExecuteAsync_combina_documentos_de_varias_fuentes_registradas()
    {
        var driveSource = new FakeDocumentSource(
            FuenteDocumento.GoogleDrive,
            new[] { Spec("d-1", "drive.pdf", PdfMime) });

        var trelloSource = new FakeDocumentSource(
            FuenteDocumento.Trello,
            new[] { Spec("t-1", "trello-card", "text/plain") });

        var pdfParser = new FakeDocumentParser(PdfMime);
        var txtParser = new FakeDocumentParser("text/plain");

        var runner = new DocumentAnalysisRunner(
            new IDocumentSource[] { driveSource, trelloSource },
            new DocumentParserRegistry(new IDocumentParser[] { pdfParser, txtParser }));

        var result = await runner.ExecuteAsync(
            new DocumentAnalysisRequest(1, 1, DateTimeOffset.UtcNow));

        Assert.Equal(2, result.Documentos.Count);
        Assert.Contains(result.Documentos, d => d.Fuente == FuenteDocumento.GoogleDrive);
        Assert.Contains(result.Documentos, d => d.Fuente == FuenteDocumento.Trello);
    }

    [Fact]
    public async Task ExecuteAsync_propaga_OperationCanceledException_y_no_la_traga()
    {
        using var cts = new CancellationTokenSource();

        var source = new FakeDocumentSource(
            FuenteDocumento.GoogleDrive,
            new[]
            {
                Spec("doc-1", "a.pdf", PdfMime),
                Spec("doc-2", "b.pdf", PdfMime)
            },
            onProyectoIdRecibido: _ => cts.Cancel());

        var runner = BuildRunner(source, new FakeDocumentParser(PdfMime));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runner.ExecuteAsync(
                new DocumentAnalysisRequest(1, 1, DateTimeOffset.UtcNow), cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_propaga_excepcion_de_la_fuente_porque_es_falla_de_lote()
    {
        // Errores como "ProyectoId sin mapeo" o "credenciales rotas" no
        // son fallas de un documento individual; deben subir hasta el
        // host para corregir la configuración.
        var source = new BrokenDocumentSource(
            new InvalidOperationException("FolderId no resuelto"));

        var runner = BuildRunner(source, new FakeDocumentParser(PdfMime));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.ExecuteAsync(
                new DocumentAnalysisRequest(1, 1, DateTimeOffset.UtcNow)));
    }

    private static DocumentAnalysisRunner BuildRunner(
        IDocumentSource source,
        params IDocumentParser[] parsers)
    {
        return new DocumentAnalysisRunner(
            new[] { source },
            new DocumentParserRegistry(parsers));
    }

    private static RawDocumentSpec Spec(string id, string archivo, string mime) =>
        new(
            IdEnFuente: id,
            NombreArchivo: archivo,
            MimeType: mime,
            Tamano: 0,
            FechaCreacion: null,
            FechaModificacion: null,
            UrlReferencia: null,
            StreamFactory: () => new MemoryStream(Array.Empty<byte>()));

    private sealed class BrokenDocumentSource : IDocumentSource
    {
        private readonly Exception _ex;
        public BrokenDocumentSource(Exception ex) => _ex = ex;
        public FuenteDocumento Fuente => FuenteDocumento.GoogleDrive;
        public async IAsyncEnumerable<RawDocument> EnumerateAsync(
            int proyectoId,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            throw _ex;
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }
    }
}
