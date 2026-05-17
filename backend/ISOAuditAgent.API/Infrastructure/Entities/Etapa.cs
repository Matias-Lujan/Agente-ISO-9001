namespace ISOAuditAgent.Infrastructure.Entities;

/// <summary>
/// Fase del ciclo de vida dentro de un procedimiento
/// (Planificación, Análisis y Diseño, Desarrollo, Testing,
/// Implementación, Seguimiento, …). Cada etapa pertenece a un único
/// procedimiento.
/// </summary>
/// <remarks>
/// Modelo alineado con <c>modelo_bd.md</c> §3 (tabla <c>Etapa</c>).
/// El campo <see cref="Orden"/> define el orden cronológico de las etapas
/// dentro del procedimiento: el agente lo usa para distinguir etapas
/// anteriores y actual (exigibles) de etapas futuras
/// (<c>PendienteEtapaFutura</c>).
/// </remarks>
public sealed class Etapa
{
    public int Id { get; set; }
    public int ProcedimientoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string? Descripcion { get; set; }

    public Procedimiento? Procedimiento { get; set; }
    public ICollection<ArtefactoEsperado> ArtefactosEsperados { get; set; } = new List<ArtefactoEsperado>();
}
