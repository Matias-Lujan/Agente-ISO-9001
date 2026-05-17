using ISOAuditAgent.Infrastructure.Data;
using ISOAuditAgent.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.Infrastructure.Repositories;

public sealed class ProyectoRepository : IProyectoRepository
{
    private readonly ISOAuditAgentDbContext _db;

    public ProyectoRepository(ISOAuditAgentDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<Proyecto?> GetByIdAsync(
        int proyectoId,
        CancellationToken cancellationToken = default)
    {
        return _db.Proyectos
            .AsNoTracking()
            .Include(p => p.Procedimiento)
            .Where(p => p.Id == proyectoId && p.Activo)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
