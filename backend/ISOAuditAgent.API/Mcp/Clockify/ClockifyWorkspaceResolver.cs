using ISOAuditAgent.Infrastructure.Entities;
using ISOAuditAgent.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.API.Mcp.Clockify;

/// <summary>
/// Resuelve el <c>clockify_workspace_id</c> del sistema. Prioridad:
/// variable de entorno → <c>ConfiguracionSistema</c>. La env var es un
/// override pensado para dev (cambiar workspace sin tocar la BD); en
/// producción debería usarse la entrada de BD vía panel admin.
/// </summary>
public interface IClockifyWorkspaceResolver
{
    /// <summary>
    /// Devuelve el workspaceId resuelto o lanza
    /// <see cref="InvalidOperationException"/> con detalle si no hay valor
    /// disponible ni en env var ni en <c>ConfiguracionSistema</c>.
    /// </summary>
    Task<string> ResolveAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
internal sealed class ClockifyWorkspaceResolver : IClockifyWorkspaceResolver
{
    private readonly ClockifyOptions _options;
    private readonly IConfiguracionSistemaRepository _config;

    public ClockifyWorkspaceResolver(
        IOptions<ClockifyOptions> options,
        IConfiguracionSistemaRepository config)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<string> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_options.WorkspaceIdEnvironmentVariable))
        {
            var fromEnv = Environment.GetEnvironmentVariable(_options.WorkspaceIdEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv.Trim();
            }
        }

        var fromDb = await _config
            .GetValorAsync(ConfiguracionSistemaClaves.ClockifyWorkspaceId, cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(fromDb)
            && !fromDb.StartsWith('<')) // descarta el placeholder "<workspace-id-pendiente>"
        {
            return fromDb.Trim();
        }

        throw new InvalidOperationException(
            $"Clockify: no se pudo resolver el workspaceId. Defina la variable de entorno " +
            $"'{_options.WorkspaceIdEnvironmentVariable}' o cargue la clave " +
            $"'{ConfiguracionSistemaClaves.ClockifyWorkspaceId}' en la tabla configuracion_sistema.");
    }
}
