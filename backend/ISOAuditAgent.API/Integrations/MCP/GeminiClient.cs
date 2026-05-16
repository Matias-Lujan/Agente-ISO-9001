using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ISOAuditAgent.API.Integrations.MCP;

/// <summary>
/// Interfaz generica para cualquier cliente de IA.
/// Si en el futuro se cambia de Gemini a otra IA,
/// solo hay que crear una nueva clase que implemente esta interfaz.
/// </summary>
public interface IAiClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}

/// <summary>
/// Implementacion de IAiClient para Google Gemini.
/// Soporta modo mock para pruebas sin consumir la API real.
///
/// Configuracion en appsettings.Development.json:
///   "Gemini": {
///     "ApiKey": "TU_KEY",
///     "Model": "gemini-2.5-flash",
///     "MockMode": true    <- true = respuesta simulada, false = Gemini real
///   }
/// </summary>
public class GeminiClient : IAiClient
{
    private const string ApiUrlTemplate =
        "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

    private const int MaxReintentos = 3;
    private const int SegundosEntreReintentos = 15;

    private readonly HttpClient _http;
    private readonly ILogger<GeminiClient> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly bool _mockMode;

    public GeminiClient(HttpClient http, ILogger<GeminiClient> logger, IConfiguration config)
    {
        _http   = http;
        _logger = logger;
        _apiKey = config["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini:ApiKey no configurada en appsettings.json");
        _model    = config["Gemini:Model"] ?? "gemini-2.5-flash";
        _mockMode = bool.TryParse(config["Gemini:MockMode"], out var mock) && mock;

        if (_mockMode)
            _logger.LogWarning(
                "GeminiClient en MODO MOCK. Para usar Gemini real poner MockMode=false en appsettings.Development.json");
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct = default)
    {
        // Modo mock: devolver respuesta simulada sin llamar a Gemini.
        // Para volver al modo real: poner MockMode=false en appsettings.Development.json.
        if (_mockMode)
        {
            _logger.LogInformation("MockMode activo - devolviendo respuesta simulada");
            await Task.Delay(500, ct);
            return GetMockResponse();
        }

        // Modo real: llamar a la API de Gemini
        var fullPrompt = $"{systemPrompt}\n\n{userMessage}";

        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = fullPrompt } } }
            },
            generationConfig = new
            {
                temperature     = 0.2,
                maxOutputTokens = 4096
            }
        };

        var json = JsonSerializer.Serialize(body);
        var url  = string.Format(ApiUrlTemplate, _model, _apiKey);

        _logger.LogDebug("Enviando pedido a Gemini | Modelo={Model}", _model);

        HttpResponseMessage response = null!;
        int intento = 0;

        while (true)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            response = await _http.PostAsync(url, content, ct);

            if (response.IsSuccessStatusCode)
                break;

            if ((int)response.StatusCode == 429 && intento < MaxReintentos)
            {
                intento++;
                _logger.LogWarning(
                    "Gemini respondio 429. Esperando {Seg}s antes de reintentar... ({N}/{Max})",
                    SegundosEntreReintentos, intento, MaxReintentos);
                await Task.Delay(TimeSpan.FromSeconds(SegundosEntreReintentos), ct);
                continue;
            }

            response.EnsureSuccessStatusCode();
        }

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        _logger.LogDebug("Respuesta recibida de Gemini ({Length} chars)", text.Length);
        return text;
    }

    /// <summary>
    /// Respuesta simulada alineada al Contrato 3 (HallazgosPreliminares).
    /// Simula hallazgos reales sobre los artefactos del demo.json:
    /// - ERS con seccion "Requerimientos no funcionales" vacia (artefactoId=2)
    /// - FR-25 con secciones "Descripcion de cambios" y "Ambientes de despliegue" vacias (artefactoId=4)
    /// Para volver a Gemini real: poner MockMode=false en appsettings.Development.json.
    /// </summary>
    private static string GetMockResponse()
    {
        return @"{
  ""hallazgos"": [
    {
      ""artefactoEsperadoId"": 2,
      ""descripcion"": ""La seccion 'Requerimientos no funcionales' del ERS existe pero no tiene contenido."",
      ""justificacion"": ""El template del ERS (FR-30) requiere que la seccion de requerimientos no funcionales describa restricciones de performance, seguridad y escalabilidad del sistema."",
      ""origenRegla"": ""Template""
    },
    {
      ""artefactoEsperadoId"": 4,
      ""descripcion"": ""El Formulario de Liberacion de Software (FR-25) tiene las secciones 'Descripcion de cambios' y 'Ambientes de despliegue' vacias."",
      ""justificacion"": ""Segun el procedimiento PR 11-13, el FR-25 debe indicar funcionalmente que se esta liberando: nuevas funcionalidades, cambios o correccion de errores, y en que ambientes se despliega."",
      ""origenRegla"": ""Procedimiento""
    },
    {
      ""artefactoEsperadoId"": 4,
      ""descripcion"": ""Inconsistencia entre el ERS y el FR-25: el ERS describe funcionalidades que no estan mencionadas en la liberacion de software."",
      ""justificacion"": ""El FR-25 debe reflejar las funcionalidades definidas en el ERS que se incluyen en el paquete de liberacion, segun el procedimiento PR 11-13."",
      ""origenRegla"": ""Procedimiento""
    }
  ]
}";
    }
}

/// <summary>
/// Helper para convertir la respuesta JSON de la IA a objetos C#.
/// Gemini a veces envuelve el JSON en bloques markdown (```json ... ```)
/// asi que los limpiamos antes de parsear.
/// </summary>
public static class AiResponseParser
{
    public static T? ParseJson<T>(string rawResponse, ILogger logger)
    {
        try
        {
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
            logger.LogError(ex,
                "No se pudo parsear la respuesta de la IA. Respuesta: {Raw}",
                rawResponse[..Math.Min(500, rawResponse.Length)]);
            return default;
        }
    }
}