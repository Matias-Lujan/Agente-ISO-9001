namespace ISOAuditAgent.Infrastructure.Repositories;

/// <summary>
/// Acceso de solo lectura a la tabla <c>configuracion_sistema</c>.
/// </summary>
public interface IConfiguracionSistemaRepository
{
    /// <summary>
    /// Devuelve el valor asociado a <paramref name="clave"/>, o
    /// <c>null</c> si no existe la entrada.
    /// </summary>
    Task<string?> GetValorAsync(string clave, CancellationToken cancellationToken = default);
}
