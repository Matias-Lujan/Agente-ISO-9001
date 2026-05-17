namespace ISOAuditAgent.DocumentAnalysis.Configuration;

/// <summary>
/// Opciones del cliente Google Drive: resolución de carpetas por proyecto,
/// MIME types permitidos y exclusiones aplicadas durante el listado recursivo.
/// </summary>
/// <remarks>
/// Alineada con <c>docs/agente-analisis-documental.md</c> §7.2-7.4.
/// La lógica de filtrado por MIME / exclusiones se implementa en Fases
/// posteriores (cliente MCP); aquí solo se modela la configuración.
/// </remarks>
public sealed class DriveOptions
{
    /// <summary>
    /// Mapeos <c>ProyectoId → FolderId</c>. Cada <see cref="ProyectoFolderMapping.ProyectoId"/>
    /// debe ser único dentro de la lista (validado al iniciar).
    /// </summary>
    public List<ProyectoFolderMapping> Mappings { get; set; } = new();

    /// <summary>
    /// Lista de MIME types aceptados durante el listado en Drive.
    /// Por defecto v1: PDF, DOCX, XLSX. Configurable / extensible.
    /// </summary>
    public List<string> MimeTypes { get; set; } = new();

    /// <summary>
    /// Reglas de exclusión por nombre de carpeta.
    /// </summary>
    public DriveExclusionOptions Exclusiones { get; set; } = new();

    /// <summary>
    /// Credenciales de autenticación con Google Drive (service account).
    /// </summary>
    public DriveAuthOptions Auth { get; set; } = new();
}
