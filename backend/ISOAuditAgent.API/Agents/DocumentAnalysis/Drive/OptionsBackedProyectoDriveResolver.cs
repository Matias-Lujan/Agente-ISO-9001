using System.Diagnostics.CodeAnalysis;
using ISOAuditAgent.DocumentAnalysis.Configuration;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Drive;

/// <summary>
/// Implementación de <see cref="IProyectoDriveResolver"/> que resuelve
/// la carpeta de Drive a partir de <see cref="DocumentAnalysisOptions"/>
/// (sección <c>"DocumentAnalysis:Drive:Mappings"</c> de
/// <c>appsettings.json</c>).
/// </summary>
/// <remarks>
/// <para>
/// Snapshot inmutable de los mapeos al construir; reaccionar a cambios
/// en runtime queda fuera del alcance v1 (la migración a BD se hace
/// reemplazando la implementación, no recargando configuración).
/// </para>
/// <para>
/// Coste de búsqueda <c>O(1)</c> mediante <see cref="Dictionary{TKey,TValue}"/>.
/// </para>
/// </remarks>
public sealed class OptionsBackedProyectoDriveResolver : IProyectoDriveResolver
{
    private readonly IReadOnlyDictionary<int, string> _mappings;

    public OptionsBackedProyectoDriveResolver(IOptions<DocumentAnalysisOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Acceso a .Value dispara las validaciones (DataAnnotations + IValidateOptions).
        var drive = options.Value.Drive;

        _mappings = drive.Mappings.ToDictionary(m => m.ProyectoId, m => m.FolderId);
    }

    public string ResolveFolderId(int proyectoId)
    {
        if (_mappings.TryGetValue(proyectoId, out var folderId))
        {
            return folderId;
        }

        throw new ProyectoDriveMappingNotFoundException(proyectoId);
    }

    public bool TryResolveFolderId(int proyectoId, [NotNullWhen(true)] out string? folderId)
    {
        return _mappings.TryGetValue(proyectoId, out folderId);
    }
}
