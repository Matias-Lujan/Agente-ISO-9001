using ISOAuditAgent.Infrastructure.Data;
using ISOAuditAgent.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.Infrastructure.Repositories;

public sealed class ArtefactoEsperadoRepository : IArtefactoEsperadoRepository
{
    private readonly ISOAuditAgentDbContext _db;

    public ArtefactoEsperadoRepository(ISOAuditAgentDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<ArtefactoEsperado>> ListarPorProcedimientoAsync(
        int procedimientoId,
        CancellationToken cancellationToken = default)
    {
        var artefactos = await _db.ArtefactosEsperados
            .AsNoTracking()
            .Include(a => a.Etapa)
            .Where(a => a.Etapa!.ProcedimientoId == procedimientoId)
            .OrderBy(a => a.Etapa!.Orden)
            .ThenBy(a => a.Codigo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return artefactos;
    }
}
