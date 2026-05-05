using ISOAuditAgent.API.Agents.ComplianceValidation;
using ISOAuditAgent.API.Integrations.MCP;
using ISOAuditAgent.API.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// ===== Configuración de Logging =====
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// ===== Configuración de Semantic Kernel =====
// Se usa Google Gemini como LLM único para análisis de inconsistencias.
var geminiApiKey = GetGeminiApiKey(builder.Configuration);
var geminiModelId = GetGeminiModelId(builder.Configuration);

builder.Services.AddKernel()
    .AddGoogleAIGeminiChatCompletion(geminiModelId, geminiApiKey);

// ===== Registro de Dependencias para el Agente de Validación =====
// Integración MCP
builder.Services.AddScoped<IMcpClient, MockMcpClient>();

// Repositorio de Reglas de Validación
builder.Services.AddScoped<IReglaValidacionRepository, MockReglaValidacionRepository>();

// Agente de Validación de Cumplimiento
builder.Services.AddScoped<IComplianceValidationAgent, ComplianceValidationAgent>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapControllers();

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
