using ISOAuditAgent.API.Internal;
using ISOAuditAgent.API.Mcp.Clockify;
using ISOAuditAgent.API.Mcp.Drive;
using ISOAuditAgent.API.Mcp.Trello;
using ISOAuditAgent.DocumentAnalysis.Extensions;
using ISOAuditAgent.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDocumentAnalysis(builder.Configuration);
builder.Services.AddGoogleDriveMcpServer();
builder.Services.AddTrelloMcpTools(builder.Configuration);
builder.Services.AddClockifyMcpTools(builder.Configuration);

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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
