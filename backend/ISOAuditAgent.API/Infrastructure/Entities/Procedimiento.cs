namespace ISOAuditAgent.Infrastructure.Entities;

/// <summary>
/// Manual maestro de BDT que define cómo se desarrolla un tipo específico
/// de proyecto (ej. PR 11-13 para Diseño, Desarrollo e Implementación de
/// Software). Agrupa las etapas del ciclo de vida y los artefactos
/// esperados en cada una.
/// </summary>
/// <remarks>
/// Modelo alineado con <c>modelo_bd.md</c> §3 (tabla <c>Procedimiento</c>).
/// En el MVP solo se usa <c>PR 11-13</c>; el modelo soporta agregar otros
/// (Software Factory, Compras, etc.) en el futuro.
/// </remarks>
public sealed class Procedimiento
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public ICollection<Etapa> Etapas { get; set; } = new List<Etapa>();
}
