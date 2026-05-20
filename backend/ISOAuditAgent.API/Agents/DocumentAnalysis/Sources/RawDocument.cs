using ISOAuditAgent.Contracts;

namespace ISOAuditAgent.DocumentAnalysis.Sources;

/// <summary>
/// Documento "crudo" obtenido de una fuente externa (Drive, Trello,
/// Clockify, …) antes de ser parseado y normalizado por DocumentAnalysis
/// (Fase 4). Combina metadatos suficientes para identificarlo en su
/// fuente y un <see cref="Stream"/> con los bytes para que el parser
/// adecuado al <see cref="MimeType"/> pueda extraer el texto.
/// </summary>
/// <remarks>
/// <para>
/// El stream entregado siempre arranca en <c>Position = 0</c>. El
/// consumidor es responsable de leerlo y disponer del <see cref="RawDocument"/>
/// (que dispondrá del stream subyacente). Al iterar
/// <see cref="IDocumentSource.EnumerateAsync"/> se recomienda envolver el
/// item en <c>using</c> dentro del <c>await foreach</c>:
/// </para>
/// <code>
/// await foreach (var raw in source.EnumerateAsync(proyectoId, ct))
/// {
///     using (raw) { ... }
/// }
/// </code>
/// <para>
/// Los nombres <c>IdEnFuente</c>, <c>NombreArchivo</c>, <c>Fuente</c> y
/// <c>UrlReferencia</c> están alineados con el contrato del agente
/// (<c>contratos_agentes.md §3.2</c>, record
/// <c>DocumentoEncontrado</c>) para minimizar mapeos al ensamblar la
/// salida.
/// </para>
/// </remarks>
public sealed record RawDocument(
    string IdEnFuente,
    string NombreArchivo,
    FuenteDocumento Fuente,
    string MimeType,
    long? Tamano,
    DateTimeOffset? FechaCreacion,
    DateTimeOffset? FechaModificacion,
    string? UrlReferencia,
    Stream Contenido
) : IDisposable
{
    /// <summary>
    /// Libera el <see cref="Stream"/> subyacente.
    /// </summary>
    public void Dispose() => Contenido.Dispose();
}
