using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Drive;

namespace ISOAuditAgent.DocumentAnalysis.Sources;

/// <summary>
/// Abstracción de "fuente de documentos" del agente DocumentAnalysis
/// (Fase 3 — §11 de la especificación). Dado un <c>ProyectoId</c>,
/// expone como <see cref="IAsyncEnumerable{T}"/> los documentos crudos
/// que deben pasar por los parsers (Fase 4) y luego por el runner
/// (Fase 5).
/// </summary>
/// <remarks>
/// <para>
/// El consumidor canónico es <see cref="DocumentAnalysisRunner"/>: se
/// inyecta una o más implementaciones de <see cref="IDocumentSource"/>
/// y el runner las recorre sin acoplarse a Drive ni a ningún MCP en
/// particular.
/// </para>
/// <para>
/// La implementación por defecto, <c>DriveDocumentSource</c>, vive en
/// el mismo proceso que el servidor MCP Drive y reutiliza
/// <see cref="IDriveListingService"/> + <see cref="IDriveClient"/>: las
/// mismas piezas que se publican como tools en <c>/mcp/drive</c>. De
/// esta forma cumplimos los dos criterios de la Fase 3:
/// </para>
/// <list type="number">
///   <item><description>
///     El <c>FolderId</c> se resuelve vía
///     <see cref="IProyectoDriveResolver"/> de manera transitiva
///     (lo invoca <see cref="IDriveListingService"/>).
///   </description></item>
///   <item><description>
///     La abstracción es directamente usable desde el
///     <see cref="DocumentAnalysisRunner"/>: el runner no sabe de Google
///     Drive, solo de <see cref="IDocumentSource"/>.
///   </description></item>
/// </list>
/// <para>
/// Si más adelante se necesita conectarse a un servidor MCP <em>remoto</em>
/// (otro proceso, otra máquina), basta agregar una segunda implementación
/// que hable JSON-RPC vía HTTP — el contrato de cara al runner no cambia.
/// </para>
/// </remarks>
public interface IDocumentSource
{
    /// <summary>
    /// Identifica de qué tipo de fuente proviene esta implementación.
    /// Se propaga directamente a
    /// <see cref="DocumentoExtraido.Fuente"/> en el contrato del
    /// orquestador.
    /// </summary>
    FuenteDocumento Fuente { get; }

    /// <summary>
    /// Enumera los documentos crudos del proyecto en orden de
    /// descubrimiento de la fuente. Cada item contiene metadatos +
    /// stream con los bytes.
    /// </summary>
    /// <remarks>
    /// El consumidor debe disponer cada <see cref="RawDocument"/>
    /// (idealmente con <c>using</c>) para liberar el stream antes de
    /// pedir el siguiente.
    /// </remarks>
    IAsyncEnumerable<RawDocument> EnumerateAsync(
        int proyectoId,
        CancellationToken cancellationToken = default);
}
