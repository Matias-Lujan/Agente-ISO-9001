using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Mcp;

namespace ISOAuditAgent.DocumentAnalysis.Sources;

/// <summary>
/// Abstracción de "fuente de documentos" del agente DocumentAnalysis.
/// Dado un <c>ProyectoId</c>, expone como <see cref="IAsyncEnumerable{T}"/>
/// los documentos crudos que el agente debe parsear y luego clasificar.
/// </summary>
/// <remarks>
/// <para>
/// La implementación por defecto, <c>DriveDocumentSource</c>, consume
/// <see cref="IDriveMcpClient"/> (HTTP Streamable hacia
/// <c>/mcp/drive</c>): el mismo contrato que las herramientas MCP expuestas
/// por el host, sin usar <see cref="IDriveClient"/> ni
/// <see cref="IDriveListingService"/> directamente en el pipeline del agente.
/// El servidor MCP, en cambio, sí usa resolver + listing + cliente Drive.
/// </para>
/// <para>
/// Si más adelante se necesita conectarse a un servidor MCP <em>remoto</em>
/// (otro proceso, otra máquina), basta agregar una segunda implementación
/// que hable JSON-RPC vía HTTP — el contrato de cara al consumidor no cambia.
/// </para>
/// </remarks>
public interface IDocumentSource
{
    /// <summary>
    /// Identifica de qué tipo de fuente proviene esta implementación.
    /// Se propaga directamente al campo <c>Fuente</c> de
    /// <c>DocumentoEncontrado</c> en el contrato del agente
    /// (<c>contratos_agentes.md §3.2</c>).
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
