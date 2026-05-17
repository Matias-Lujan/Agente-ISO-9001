namespace ISOAuditAgent.DocumentAnalysis.Configuration;

/// <summary>
/// Credenciales para autenticarse con Google Drive vía cuenta de servicio
/// (§7.1 de la especificación).
/// </summary>
/// <remarks>
/// <para>
/// Sólo uno de <see cref="ServiceAccountKeyPath"/> /
/// <see cref="ServiceAccountKeyJson"/> debe estar definido. Si ninguno
/// está definido, el módulo se configura como "no autenticado":
/// la validación pasa pero cualquier llamada real al cliente real fallará
/// con un mensaje explícito (útil para tests unitarios sin red).
/// </para>
/// <para>
/// <see cref="ServiceAccountKeyJson"/> está pensado para entornos donde
/// el JSON de la service account llega por variable de entorno
/// (<c>DocumentAnalysis__Drive__Auth__ServiceAccountKeyJson</c>), evitando
/// commitear archivos sensibles.
/// </para>
/// </remarks>
public sealed class DriveAuthOptions
{
    /// <summary>
    /// Path absoluto o relativo al JSON de credenciales de service account.
    /// </summary>
    public string? ServiceAccountKeyPath { get; set; }

    /// <summary>
    /// Contenido inline del JSON de credenciales de service account.
    /// </summary>
    public string? ServiceAccountKeyJson { get; set; }

    /// <summary>
    /// <c>ApplicationName</c> reportado a Google al iniciar el cliente Drive.
    /// </summary>
    public string ApplicationName { get; set; } = "ISOAuditAgent";

    /// <summary>
    /// Indica si hay alguna credencial configurada.
    /// </summary>
    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(ServiceAccountKeyPath) ||
        !string.IsNullOrWhiteSpace(ServiceAccountKeyJson);
}
