using System.ComponentModel.DataAnnotations;

namespace ISOAuditAgent.DocumentAnalysis.Configuration;

/// <summary>
/// Asociación 1:1 entre un <c>ProyectoId</c> de la BD y la <c>FolderId</c>
/// raíz del proyecto en Google Drive.
/// </summary>
/// <remarks>
/// Implementación v1 desde <c>appsettings.json</c>. Cuando migremos a MySQL
/// (§7.2), la firma de <see cref="Drive.IProyectoDriveResolver"/> no cambia:
/// solo se reemplaza la implementación que provee estos pares.
/// </remarks>
public sealed class ProyectoFolderMapping
{
    /// <summary>
    /// Identificador del proyecto (PK autoincremental en BD).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "ProyectoId debe ser mayor a cero.")]
    public int ProyectoId { get; set; }

    /// <summary>
    /// FolderId raíz en Google Drive compartido con la cuenta de servicio.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "FolderId no puede ser vacío.")]
    public string FolderId { get; set; } = string.Empty;
}
