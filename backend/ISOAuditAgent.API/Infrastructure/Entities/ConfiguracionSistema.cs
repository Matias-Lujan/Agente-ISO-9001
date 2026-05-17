namespace ISOAuditAgent.Infrastructure.Entities;

/// <summary>
/// Configuración global del sistema almacenada como par clave-valor.
/// Permite cambiar valores operativos sin tocar código ni redeployar.
/// </summary>
/// <remarks>
/// Modelo alineado con <c>modelo_bd.md</c> §3 (tabla
/// <c>ConfiguracionSistema</c>). En el MVP se usa principalmente para
/// almacenar la clave <c>drive_carpeta_templates_id</c> (FolderId de
/// Drive donde viven los templates de los FRs).
/// </remarks>
public sealed class ConfiguracionSistema
{
    public int Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

/// <summary>
/// Claves conocidas en <see cref="ConfiguracionSistema"/>. Centralizar
/// las constantes evita errores de tipeo dispersos.
/// </summary>
public static class ConfiguracionSistemaClaves
{
    /// <summary>
    /// FolderId de Drive donde viven los templates de los FRs.
    /// Se compone con <c>ArtefactoEsperado.TemplateDriveFilename</c>
    /// para localizar cada template específico.
    /// </summary>
    public const string DriveCarpetaTemplatesId = "drive_carpeta_templates_id";
}
