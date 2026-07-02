// ============================================================================
//  McpDriveClientOptions — Configuración del cliente MCP de Drive (D3.2)
// ----------------------------------------------------------------------------
//  Bind desde la sección "Mcp:Drive" de appsettings. Define el endpoint del
//  server MCP local que el DriveMcpSdkClient consume.
//
//  Por qué configurable y no hardcoded: en D3.1 el smoke asumía
//  http://localhost:5180/mcp/drive. Pasar a config hace al cliente robusto
//  ante cambios de puerto (otro launchSettings, otro entorno).
// ============================================================================

namespace ISOAuditAgent.API.Integrations.MCP.Drive;

public sealed class McpDriveClientOptions
{
    public const string SectionName = "Mcp:Drive";

    /// <summary>
    /// URL absoluta del server MCP de Drive en este mismo proceso.
    /// Default razonable para dev: http://localhost:5180/mcp/drive.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:5180/mcp/drive";
}