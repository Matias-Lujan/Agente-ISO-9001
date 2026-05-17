using ISOAuditAgent.Infrastructure.Data;
using ISOAuditAgent.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.Infrastructure.Repositories;

public sealed class EtapaRepository : IEtapaRepository
{
    private readonly ISOAuditAgentDbContext _db;

    public EtapaRepository(ISOAuditAgentDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<Etapa?> GetByIdAsync(int etapaId, CancellationToken cancellationToken = default)
    {
        return _db.Etapas
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == etapaId, cancellationToken);
    }

    public async Task<IReadOnlyList<Etapa>> ListarPorProcedimientoAsync(
        int procedimientoId,
        CancellationToken cancellationToken = default)
    {
        var etapas = await _db.Etapas
            .AsNoTracking()
            .Where(e => e.ProcedimientoId == procedimientoId)
            .OrderBy(e => e.Orden)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return etapas;
    }
}
