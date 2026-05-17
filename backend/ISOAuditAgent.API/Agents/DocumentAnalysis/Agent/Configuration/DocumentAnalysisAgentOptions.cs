using System.ComponentModel.DataAnnotations;

namespace ISOAuditAgent.DocumentAnalysis.Agent.Configuration;

public sealed class DocumentAnalysisAgentOptions
{
    public const string SectionName = "DocumentAnalysisAgent";

    [Required]
    public LlmClientOptions Llm { get; set; } = new();

    /// <summary>
    /// Reintentos adicionales tras fallar el validador de invariantes (total de intentos = 1 + este valor).
    /// </summary>
    [Range(0, 10)]
    public int MaxLlmRetries { get; set; } = 2;

    [Range(0, 2)]
    public float TemperatureLow { get; set; } = 0.1f;
}

public sealed class LlmClientOptions
{
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    [Required]
    public string ApiKeyEnvironmentVariable { get; set; } = "GEMINI_API_KEY";
}
