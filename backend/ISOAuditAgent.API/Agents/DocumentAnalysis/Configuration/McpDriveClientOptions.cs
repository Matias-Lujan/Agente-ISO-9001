using System.ComponentModel.DataAnnotations;
using ISOAuditAgent.DocumentAnalysis.Mcp;
using ModelContextProtocol.Client;

namespace ISOAuditAgent.DocumentAnalysis.Configuration;

/// <summary>
/// Configuración del cliente MCP hacia el servidor Drive del propio host
/// (<c>/mcp/drive</c>), usado por <see cref="DriveMcpSdkClient"/>.
/// </summary>
public sealed class McpDriveClientOptions
{
    public const string SectionName = "McpDrive";

    /// <summary>
    /// URL absoluta del endpoint MCP (p. ej. <c>http://localhost:5180/mcp/drive</c>).
    /// </summary>
    [Required]
    [Url]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Modo HTTP del transporte MCP (por defecto Streamable HTTP, alineado al servidor).
    /// </summary>
    public HttpTransportMode TransportMode { get; set; } = HttpTransportMode.StreamableHttp;
}
