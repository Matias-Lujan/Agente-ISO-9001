// ============================================================================
//  GeminiOptions — Configuración del provider Gemini (D4)
// ----------------------------------------------------------------------------
//  Bind desde la sección "Gemini" de la configuración.
//
//  ApiKey: NUNCA commiteada. Se setea por dotnet user-secrets:
//      dotnet user-secrets set "Gemini:ApiKey" "<tu-api-key>"
//  En appsettings.Development.json queda solo el ModelId (y opcionalmente
//  ApiKey vacío como marcador).
//
//  ModelId: por defecto gemini-2.5-flash. Free tier de Google AI Studio
//  alcanza para todos los nodos del MVP. Si en E2E algún nodo necesita
//  más razonamiento, se puede agregar override por nodo en D7.
// ============================================================================

namespace ISOAuditAgent.API.Integrations.LLM;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>
    /// API key de Google AI Studio. SET POR user-secrets, no por archivo.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Modelo de Gemini a usar. Default: gemini-2.5-flash (free tier).
    /// </summary>
    public string ModelId { get; set; } = "gemini-2.5-flash";
}