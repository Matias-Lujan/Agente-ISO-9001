using ISOAuditAgent.API.Agents.ConsistencyVerification;
using ISOAuditAgent.API.Integrations.MCP;
using ISOAuditAgent.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── OpenAPI (sistema nativo de .NET 9) ────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Servicios del agente ConsistencyVerification ──────────────────────────────
builder.Services.AddHttpClient<IAiClient, GeminiClient>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
    client.Timeout = TimeSpan.FromSeconds(120);
});
builder.Services.AddSingleton<IDocumentSummaryBuilder, DocumentSummaryBuilder>();
builder.Services.AddScoped<ConsistencyVerificationAgentService>();

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Esto permite que los enums se lean y escriban como texto ("Exigible")
        // en lugar de numeros (0, 1, 2)
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (app.Urls.Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();