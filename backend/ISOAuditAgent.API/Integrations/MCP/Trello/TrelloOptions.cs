namespace ISOAuditAgent.API.Integrations.MCP.Trello;

public sealed class TrelloOptions
{
    public const string SectionName = "Trello";

    public string ApiKey { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
