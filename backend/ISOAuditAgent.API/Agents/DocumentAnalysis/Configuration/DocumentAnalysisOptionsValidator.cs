using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Configuration;

/// <summary>
/// Validador estructural de <see cref="DocumentAnalysisOptions"/>.
/// Cubre las reglas que el atributo <c>ValidateDataAnnotations()</c> sobre
/// la opción raíz no aplica recursivamente (elementos de listas) más las
/// reglas que <c>DataAnnotations</c> no puede expresar:
/// <list type="bullet">
///   <item><description>cada <see cref="ProyectoFolderMapping.ProyectoId"/> debe ser positivo;</description></item>
///   <item><description>cada <see cref="ProyectoFolderMapping.FolderId"/> no puede ser vacío ni whitespace;</description></item>
///   <item><description>los <see cref="ProyectoFolderMapping.ProyectoId"/> deben ser únicos dentro de la lista;</description></item>
///   <item><description>cada patrón en <see cref="DriveExclusionOptions.PatronesRegex"/> debe ser un regex .NET válido.</description></item>
/// </list>
/// </summary>
internal sealed class DocumentAnalysisOptionsValidator
    : IValidateOptions<DocumentAnalysisOptions>
{
    public ValidateOptionsResult Validate(string? name, DocumentAnalysisOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();
        var drive = options.Drive;

        for (var i = 0; i < drive.Mappings.Count; i++)
        {
            var mapping = drive.Mappings[i];

            if (mapping.ProyectoId <= 0)
            {
                errors.Add(
                    $"DocumentAnalysis:Drive:Mappings[{i}].ProyectoId debe ser " +
                    $"mayor a cero (valor actual: {mapping.ProyectoId}).");
            }

            if (string.IsNullOrWhiteSpace(mapping.FolderId))
            {
                errors.Add(
                    $"DocumentAnalysis:Drive:Mappings[{i}].FolderId no puede ser vacío.");
            }
        }

        var duplicados = drive.Mappings
            .Where(m => m.ProyectoId > 0)
            .GroupBy(m => m.ProyectoId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicados.Count > 0)
        {
            errors.Add(
                "DocumentAnalysis:Drive:Mappings contiene ProyectoId duplicados: " +
                string.Join(", ", duplicados) + ".");
        }

        for (var i = 0; i < drive.Exclusiones.PatronesRegex.Count; i++)
        {
            var patron = drive.Exclusiones.PatronesRegex[i];
            try
            {
                _ = new Regex(patron, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            }
            catch (ArgumentException ex)
            {
                errors.Add(
                    $"DocumentAnalysis:Drive:Exclusiones:PatronesRegex[{i}] " +
                    $"no es un regex .NET válido ('{patron}'): {ex.Message}");
            }
        }

        var auth = drive.Auth;
        var hasPath = !string.IsNullOrWhiteSpace(auth.ServiceAccountKeyPath);
        var hasJson = !string.IsNullOrWhiteSpace(auth.ServiceAccountKeyJson);

        if (hasPath && hasJson)
        {
            errors.Add(
                "DocumentAnalysis:Drive:Auth no admite definir " +
                "ServiceAccountKeyPath y ServiceAccountKeyJson simultáneamente.");
        }

        if (hasPath && !File.Exists(auth.ServiceAccountKeyPath))
        {
            errors.Add(
                $"DocumentAnalysis:Drive:Auth:ServiceAccountKeyPath apunta a un " +
                $"archivo inexistente: '{auth.ServiceAccountKeyPath}'.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
