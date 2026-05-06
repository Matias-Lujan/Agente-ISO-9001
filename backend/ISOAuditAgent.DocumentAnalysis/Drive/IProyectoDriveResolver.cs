using System.Diagnostics.CodeAnalysis;

namespace ISOAuditAgent.DocumentAnalysis.Drive;

/// <summary>
/// Resuelve el <c>FolderId</c> raíz en Google Drive a partir del
/// <c>ProyectoId</c> de la BD del sistema.
/// </summary>
/// <remarks>
/// <para>
/// Implementación v1: lee desde <c>appsettings.json</c> (vía Options).
/// Implementación futura: tabla MySQL. La firma de esta interfaz no
/// debe cambiar entre versiones (§7.2 de la especificación).
/// </para>
/// <para>
/// Los consumidores tienen dos contratos disponibles para el manejo de
/// faltantes: <see cref="ResolveFolderId(int)"/> (lanza excepción tipada)
/// y <see cref="TryResolveFolderId(int, out string?)"/> (no lanza).
/// </para>
/// </remarks>
public interface IProyectoDriveResolver
{
    /// <summary>
    /// Devuelve el <c>FolderId</c> asociado a <paramref name="proyectoId"/>.
    /// </summary>
    /// <exception cref="ProyectoDriveMappingNotFoundException">
    /// No existe un mapeo configurado para el <paramref name="proyectoId"/>
    /// solicitado.
    /// </exception>
    string ResolveFolderId(int proyectoId);

    /// <summary>
    /// Intenta resolver el <c>FolderId</c> sin lanzar excepción.
    /// </summary>
    /// <param name="proyectoId">Identificador del proyecto.</param>
    /// <param name="folderId">FolderId resuelto, o <c>null</c> si no existe mapeo.</param>
    /// <returns><c>true</c> si se encontró el mapeo; <c>false</c> en caso contrario.</returns>
    bool TryResolveFolderId(int proyectoId, [NotNullWhen(true)] out string? folderId);
}
