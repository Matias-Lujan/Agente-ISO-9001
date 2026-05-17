namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Define el estado de exigibilidad de un artefacto en la auditoría ISO 9001.
/// Indica si el artefacto es obligatorio en la etapa actual o si será requerido en una etapa futura.
/// </summary>
public enum ExigibilidadArtefacto
{
    /// <summary>
    /// El artefacto es exigible y debe estar presente en esta etapa de auditoría.
    /// </summary>
    Exigible = 1,

    /// <summary>
    /// El artefacto es exigible en una etapa futura de la auditoría, no en esta.
    /// </summary>
    PendienteEtapaFutura = 2
}
