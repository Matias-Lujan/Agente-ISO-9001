namespace ISOAuditAgent.API.Models;

/// <summary>
/// Constantes de roles para usar en los atributos [Authorize].
/// Necesario porque [Authorize(Roles = ...)] requiere un string constante.
/// </summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Operador      = "Operador";
    public const string Auditor       = "Auditor";
}