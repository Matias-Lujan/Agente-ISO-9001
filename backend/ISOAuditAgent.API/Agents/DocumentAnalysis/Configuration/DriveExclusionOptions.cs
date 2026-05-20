namespace ISOAuditAgent.DocumentAnalysis.Configuration;

/// <summary>
/// Reglas de exclusión por nombre de carpeta (§7.4 de la especificación).
/// </summary>
/// <remarks>
/// La lógica que aplica estas reglas al listado recursivo se implementa
/// en Fases 2-3 (servidor / cliente MCP). En Fase 1 sólo se modela y bindea.
/// </remarks>
public sealed class DriveExclusionOptions
{
    /// <summary>
    /// Nombres exactos de carpetas a excluir (comparación case-sensitive
    /// salvo que la implementación final indique lo contrario).
    /// Ejemplos típicos: <c>"Archivo"</c>, <c>"Backup"</c>.
    /// </summary>
    public List<string> NombresExactos { get; set; } = new();

    /// <summary>
    /// Patrones regex aplicados al nombre de carpeta. Cualquier match
    /// implica exclusión. Ejemplos: <c>".*archivo.*"</c>, <c>".*backup.*"</c>.
    /// Cada patrón debe ser un regex .NET válido (validado al iniciar).
    /// </summary>
    public List<string> PatronesRegex { get; set; } = new();
}
