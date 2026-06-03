namespace ISOAuditAgent.API.Integrations.MCP.Clockify;

public sealed class McpClockifyClientOptions
{
    public const string SectionName = "Mcp:Clockify";

    public string Endpoint { get; set; } = "http://localhost:5180/mcp/clockify";
}
