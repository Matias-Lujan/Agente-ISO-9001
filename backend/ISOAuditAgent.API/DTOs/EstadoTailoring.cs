namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Define el estado de aplicabilidad de una regla de tailoring.
/// Indica si una regla específica aplica al proyecto o si no aplica en el contexto actual.
/// </summary>
public enum EstadoTailoring
{
    /// <summary>
    /// La regla de tailoring aplica al proyecto actual.
    /// </summary>
    Aplica = 1,

    /// <summary>
    /// La regla de tailoring no aplica al proyecto actual.
    /// </summary>
    NoAplica = 2
}
