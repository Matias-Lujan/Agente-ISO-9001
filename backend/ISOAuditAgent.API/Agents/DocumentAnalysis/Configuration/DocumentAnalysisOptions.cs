namespace ISOAuditAgent.DocumentAnalysis.Configuration;

/// <summary>
/// Raíz de configuración del módulo DocumentAnalysis.
/// Se bindea desde la sección <c>"DocumentAnalysis"</c> de
/// <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// Alineada con <c>docs/agente-analisis-documental.md</c> §7.
/// La configuración es POCO mutable a propósito: el binder de
/// <c>Microsoft.Extensions.Configuration</c> requiere setters públicos.
/// </remarks>
public sealed class DocumentAnalysisOptions
{
    /// <summary>
    /// Nombre de la sección en <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "DocumentAnalysis";

    /// <summary>
    /// Opciones del cliente Drive: mapeos por proyecto, MIME permitidos
    /// y exclusiones de carpetas.
    /// </summary>
    public DriveOptions Drive { get; set; } = new();
}
