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
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();