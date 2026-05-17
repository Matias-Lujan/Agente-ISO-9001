using System.Text.RegularExpressions;
using ISOAuditAgent.DocumentAnalysis.Configuration;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Drive;

/// <summary>
/// Política de exclusión de carpetas durante el listado recursivo de Drive
/// (§7.4 de la especificación). Aplica nombres exactos y patrones regex
/// pre-compilados.
/// </summary>
/// <remarks>
/// Construido una sola vez por instancia (singleton) a partir de
/// <see cref="DriveExclusionOptions"/>. Los regex pre-compilados usan
/// <see cref="RegexOptions.IgnoreCase"/> y un timeout corto (100 ms) para
/// proteger contra patrones patológicos. Las comparaciones por nombre exacto
/// son <c>OrdinalIgnoreCase</c> (alineado con la convención de Drive de
/// nombres de carpeta no sensibles a la caja).
/// </remarks>
public sealed class DriveExclusionPolicy
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private readonly HashSet<string> _exactNames;
    private readonly Regex[] _patterns;

    public DriveExclusionPolicy(IOptions<DocumentAnalysisOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options)))
              .Value.Drive.Exclusiones)
    {
    }

    public DriveExclusionPolicy(DriveExclusionOptions exclusionOptions)
    {
        ArgumentNullException.ThrowIfNull(exclusionOptions);

        _exactNames = new HashSet<string>(
            exclusionOptions.NombresExactos,
            StringComparer.OrdinalIgnoreCase);

        _patterns = exclusionOptions.PatronesRegex
            .Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout))
            .ToArray();
    }

    /// <summary>
    /// Devuelve <c>true</c> si la carpeta debe omitirse durante la traversal
    /// (y por extensión, todo su subárbol).
    /// </summary>
    public bool IsFolderExcluded(string folderName)
    {
        ArgumentNullException.ThrowIfNull(folderName);

        if (_exactNames.Contains(folderName))
        {
            return true;
        }

        foreach (var pattern in _patterns)
        {
            if (pattern.IsMatch(folderName))
            {
                return true;
            }
        }

        return false;
    }
}
