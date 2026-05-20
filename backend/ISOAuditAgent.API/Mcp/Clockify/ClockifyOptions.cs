using System.ComponentModel.DataAnnotations;

namespace ISOAuditAgent.API.Mcp.Clockify;

/// <summary>
/// Configuración del servidor MCP de Clockify (Fase F.5.2).
/// Credenciales y workspace por variables de entorno cuando aplica.
/// </summary>
public sealed class ClockifyOptions
{
    public const string SectionName = "DocumentAnalysis:Clockify";

    /// <summary>
    /// Base URL del API REST de Clockify. Default: <c>https://api.clockify.me/api/v1</c>.
    /// </summary>
    [Required]
    [Url]
    public string ApiBaseUrl { get; set; } = "https://api.clockify.me/api/v1";

    /// <summary>
    /// Nombre de la variable de entorno que contiene la API key de Clockify
    /// (perfil → Settings → API). Default: <c>CLOCKIFY_API_KEY</c>.
    /// </summary>
    [Required]
    public string ApiKeyEnvironmentVariable { get; set; } = "CLOCKIFY_API_KEY";

    /// <summary>
    /// Nombre de la variable de entorno que <b>opcionalmente</b> sobreescribe
    /// el <c>clockify_workspace_id</c> de <c>ConfiguracionSistema</c>.
    /// Pensada para dev: permite cambiar de workspace sin tocar la BD.
    /// Default: <c>CLOCKIFY_WORKSPACE_ID</c>.
    /// </summary>
    public string WorkspaceIdEnvironmentVariable { get; set; } = "CLOCKIFY_WORKSPACE_ID";
}
