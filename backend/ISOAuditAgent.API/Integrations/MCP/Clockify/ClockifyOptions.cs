namespace ISOAuditAgent.API.Integrations.MCP.Clockify;

public sealed class ClockifyOptions
{
    public const string SectionName = "Clockify";

    public string ApiKey { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
}
