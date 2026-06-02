namespace ISOAuditAgent.API.Integrations.MCP.Trello;

public sealed class McpTrelloClientOptions
{
    public const string SectionName = "Mcp:Trello";

    public string Endpoint { get; set; } = "http://localhost:5180/mcp/trello";
}
