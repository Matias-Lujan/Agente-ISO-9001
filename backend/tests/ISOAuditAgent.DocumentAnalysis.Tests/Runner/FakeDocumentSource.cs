using System.Runtime.CompilerServices;
using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Sources;

namespace ISOAuditAgent.DocumentAnalysis.Tests.Runner;

/// <summary>
/// Fuente de documentos en memoria para tests del runner: no toca
/// Drive ni red. Cada item tiene <c>StreamFactory</c> en lugar de un
/// stream concreto, para que múltiples corridas con la misma instancia
/// no compartan estado de lectura.
/// </summary>
internal sealed class FakeDocumentSource : IDocumentSource
{
    private readonly FuenteDocumento _fuente;
    private readonly IReadOnlyList<RawDocumentSpec> _items;
    private readonly Action<int>? _onProyectoIdRecibido;

    public FakeDocumentSource(
        FuenteDocumento fuente,
        IEnumerable<RawDocumentSpec> items,
        Action<int>? onProyectoIdRecibido = null)
    {
        _fuente = fuente;
        _items = items.ToList();
        _onProyectoIdRecibido = onProyectoIdRecibido;
    }

    public FuenteDocumento Fuente => _fuente;

    public async IAsyncEnumerable<RawDocument> EnumerateAsync(
        int proyectoId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _onProyectoIdRecibido?.Invoke(proyectoId);

        foreach (var spec in _items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            yield return new RawDocument(
                IdEnFuente: spec.IdEnFuente,
                NombreArchivo: spec.NombreArchivo,
                Fuente: _fuente,
                MimeType: spec.MimeType,
                Tamano: spec.Tamano,
                FechaCreacion: spec.FechaCreacion,
                FechaModificacion: spec.FechaModificacion,
                UrlReferencia: spec.UrlReferencia,
                Contenido: spec.StreamFactory());
        }
    }
}

internal sealed record RawDocumentSpec(
    string IdEnFuente,
    string NombreArchivo,
    string MimeType,
    long? Tamano,
    DateTimeOffset? FechaCreacion,
    DateTimeOffset? FechaModificacion,
    string? UrlReferencia,
    Func<Stream> StreamFactory);
