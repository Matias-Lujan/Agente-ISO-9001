namespace ISOAuditAgent.API.Integrations.MCP.Drive;

public sealed class GoogleDriveOptions
{
    public const string SectionName = "GoogleDrive";

    public string ServiceAccountKeyPath { get; set; } = "secrets/google-service-account.json";

    public string ApplicationName { get; set; } = "ISOAuditAgent";
}