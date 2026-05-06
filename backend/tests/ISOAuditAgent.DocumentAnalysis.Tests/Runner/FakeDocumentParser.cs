using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Parsing;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Runner;

/// <summary>
/// Parser configurable para tests: traduce un MIME en un texto fijo, o
/// lanza una excepción si así se configuró. Permite simular casos de
/// éxito y de fallo aislados del SDK de OpenXml/PdfPig.
/// </summary>
internal sealed class FakeDocumentParser : IDocumentParser
{
    private readonly string _mime;
    private readonly Func<Stream, string, DocumentParseResult> _factory;
    private readonly Exception? _throwOn;

    public FakeDocumentParser(
        string mime,
        Func<Stream, string, DocumentParseResult>? factory = null,
        Exception? throwOn = null)
    {
        _mime = mime;
        _factory = factory ?? ((_, n) =>
            new DocumentParseResult($"contenido-{n}", FormatoContenido.PlainText));
        _throwOn = throwOn;
    }

    public IReadOnlyCollection<string> SupportedMimeTypes => new[] { _mime };

    public Task<DocumentParseResult> ParseAsync(
        Stream contenido,
        string nombreArchivo,
        CancellationToken cancellationToken = default)
    {
        if (_throwOn is not null)
        {
            throw _throwOn;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_factory(contenido, nombreArchivo));
    }
}
