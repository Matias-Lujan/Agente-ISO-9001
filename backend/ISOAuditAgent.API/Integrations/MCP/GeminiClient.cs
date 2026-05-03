using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ISOAuditAgent.API.Integrations.MCP;

/// <summary>
/// Interfaz genérica para cualquier cliente de IA.
/// Si en el futuro se cambia de Gemini a otra IA,
/// solo hay que crear una nueva clase que implemente esta interfaz.
/// </summary>
public interface IAiClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}

/// <summary>
/// Implementación de IAiClient para Google Gemini.
/// </summary>
public class GeminiClient : IAiClient
{
    private const string Model = "gemini-1.5-flash";
    private const string ApiUrlTemplate =
        "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

    private readonly HttpClient _http;
    private readonly ILogger<GeminiClient> _logger;
    private readonly string _apiKey;

    public GeminiClient(HttpClient http, ILogger<GeminiClient> logger, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey no configurada en appsettings.json");
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct = default)
    {
        // Combinamos systemPrompt y userMessage en un solo texto
        // porque así funciona Gemini 1.5 Flash
        var fullPrompt = $"{systemPrompt}\n\n{userMessage}";

        // Formato JSON que espera la API de Gemini
        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = fullPrompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 4096
            }
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var url = string.Format(ApiUrlTemplate, Model, _apiKey);

        _logger.LogDebug("Enviando pedido a Gemini, modelo={Model}", Model);

        var response = await _http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        // Formato de respuesta de Gemini:
        // { "candidates": [ { "content": { "parts": [ { "text": "respuesta" } ] } } ] }
        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct),
            cancellationToken: ct);

        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        _logger.LogDebug("Respuesta recibida de Gemini ({Length} chars)", text.Length);
        return text;
    }
}

/// <summary>
/// Helper para convertir la respuesta JSON de la IA a objetos C#.
/// Es genérico — funciona igual sin importar qué IA se use.
/// </summary>
public static class AiResponseParser
{
    public static T? ParseJson<T>(string rawResponse, ILogger logger)
    {
        try
        {
            // Gemini a veces envuelve el JSON en bloques markdown
            // esto los limpia antes de parsear
            var cleaned = rawResponse
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            return JsonSerializer.Deserialize<T>(cleaned, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            logger.LogError(
                ex,
                "No se pudo parsear la respuesta de la IA. Respuesta: {Raw}",
                rawResponse[..Math.Min(500, rawResponse.Length)]);
            return default;
        }
    }
}