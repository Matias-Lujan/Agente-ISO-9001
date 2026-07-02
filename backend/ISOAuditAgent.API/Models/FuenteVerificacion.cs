namespace ISOAuditAgent.API.Models;

/// <summary>
/// Fuente donde se verifica físicamente la evidencia de un artefacto.
/// Guardado como TEXT en BD (HasConversion). Sumar una fuente futura =
/// agregar valor acá + checker en Agents/DocumentAnalysis/{Fuente}/.
/// </summary>
public enum FuenteVerificacion
{
    Drive,
    Trello,
    Clockify
}
