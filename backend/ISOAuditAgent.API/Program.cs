using ISOAuditAgent.API.Agents.ComplianceValidation;
using ISOAuditAgent.API.Agents.ConsistencyVerification;
using ISOAuditAgent.API.Integrations.MCP;
using ISOAuditAgent.API.Internal;
using ISOAuditAgent.API.Mcp.Clockify;
using ISOAuditAgent.API.Mcp.Drive;
using ISOAuditAgent.API.Mcp.Trello;
using ISOAuditAgent.API.Repositories;
using ISOAuditAgent.API.Services;
using ISOAuditAgent.DocumentAnalysis.Extensions;
using ISOAuditAgent.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuración de Logging =====
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddOpenApi();

// Controllers — se enumeran los enums como string ("Exigible") en lugar de número.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDocumentAnalysis(builder.Configuration);
builder.Services.AddGoogleDriveMcpServer();
builder.Services.AddTrelloMcpTools(builder.Configuration);
builder.Services.AddClockifyMcpTools(builder.Configuration);

// ===== Configuración de Semantic Kernel (Compliance Validation Agent) =====
// Se usa Google Gemini como LLM único para análisis de inconsistencias.
var geminiApiKey = GetGeminiApiKey(builder.Configuration);
var geminiModelId = GetGeminiModelId(builder.Configuration);

builder.Services.AddKernel()
    .AddGoogleAIGeminiChatCompletion(geminiModelId, geminiApiKey);

// ===== Registro de Dependencias para el Agente de Validación =====
builder.Services.AddScoped<IMcpClient, MockMcpClient>();
builder.Services.AddScoped<IReglaValidacionRepository, MockReglaValidacionRepository>();
builder.Services.AddScoped<IComplianceValidationAgent, ComplianceValidationAgent>();

// ===== Servicios del Agente de Verificación de Consistencia =====
builder.Services.AddHttpClient<IAiClient, GeminiClient>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
    client.Timeout = TimeSpan.FromSeconds(120);
});
builder.Services.AddSingleton<IDocumentSummaryBuilder, DocumentSummaryBuilder>();
builder.Services.AddScoped<ConsistencyVerificationAgentService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDocumentSourcePreview();
    app.MapDocumentAnalysisAgentDev();
}

app.UseHttpsRedirection();

// Servidor MCP de Google Drive bajo /mcp/drive (§7.5 de la especificación).
app.MapMcp("/mcp/drive");

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

static string GetGeminiApiKey(IConfiguration config)
{
    var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (!string.IsNullOrWhiteSpace(apiKey) && !IsPlaceholderValue(apiKey))
    {
        return apiKey.Trim();
    }

    apiKey = config["GeminiApiKey"];
    if (!string.IsNullOrWhiteSpace(apiKey) && !IsPlaceholderValue(apiKey))
    {
        return apiKey.Trim();
    }

    throw new InvalidOperationException(
        "La clave Gemini API no está configurada correctamente. " +
        "Defina GEMINI_API_KEY como variable de entorno o actualice appsettings.json sin valores de plantilla."
    );
}

static string GetGeminiModelId(IConfiguration config)
{
    string? modelId = Environment.GetEnvironmentVariable("GEMINI_MODEL_ID");
    if (!string.IsNullOrWhiteSpace(modelId) && !IsPlaceholderValue(modelId))
    {
        return NormalizeGeminiModelId(modelId);
    }

    modelId = config["GeminiModelId"];
    if (!string.IsNullOrWhiteSpace(modelId) && !IsPlaceholderValue(modelId))
    {
        return NormalizeGeminiModelId(modelId);
    }

    return "gemini-2.5-flash";
}

static string NormalizeGeminiModelId(string modelId)
{
    var normalized = modelId.Trim();
    if (normalized.StartsWith("models/", StringComparison.OrdinalIgnoreCase))
    {
        normalized = normalized[("models/").Length..];
    }

    return normalized;
}

static bool IsPlaceholderValue(string value)
{
    var normalized = value.Trim();
    return normalized.StartsWith("{{") && normalized.EndsWith("}}")
        || normalized.Contains("YOUR_")
        || normalized.Contains("REPLACE_")
        || normalized.Contains("API_KEY");
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
