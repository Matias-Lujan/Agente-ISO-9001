using System.ComponentModel.DataAnnotations;

namespace ISOAuditAgent.API.Mcp.Trello;

/// <summary>
/// Configuración del servidor MCP de Trello (Fase F.5.1).
/// Credenciales reales por variables de entorno — no se commitean.
/// </summary>
public sealed class TrelloOptions
{
    public const string SectionName = "DocumentAnalysis:Trello";

    /// <summary>
    /// Base URL del API REST de Trello. Default: <c>https://api.trello.com/1</c>.
    /// </summary>
    [Required]
    [Url]
    public string ApiBaseUrl { get; set; } = "https://api.trello.com/1";

    /// <summary>
    /// Nombre de la variable de entorno que contiene la API key de Trello
    /// (https://trello.com/app-key). Default: <c>TRELLO_API_KEY</c>.
    /// </summary>
    [Required]
    public string ApiKeyEnvironmentVariable { get; set; } = "TRELLO_API_KEY";

    /// <summary>
    /// Nombre de la variable de entorno que contiene el token de usuario
    /// asociado a la API key. Default: <c>TRELLO_TOKEN</c>.
    /// </summary>
    [Required]
    public string TokenEnvironmentVariable { get; set; } = "TRELLO_TOKEN";
}
