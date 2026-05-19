using ISOAuditAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.Infrastructure.Repositories;

public sealed class ConfiguracionSistemaRepository : IConfiguracionSistemaRepository
{
    private readonly ISOAuditAgentDbContext _db;

    public ConfiguracionSistemaRepository(ISOAuditAgentDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<string?> GetValorAsync(
        string clave,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);

        return await _db.ConfiguracionesSistema
            .AsNoTracking()
            .Where(c => c.Clave == clave)
            .Select(c => c.Valor)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
