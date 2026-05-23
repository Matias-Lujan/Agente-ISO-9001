// ============================================================================
//  HallazgoRepository — Inserción de lote de Hallazgo
// ----------------------------------------------------------------------------
//  Implementa IHallazgoRepository. Consumido por AuditoriaPersistenceService
//  como segundo paso del flujo de persistencia. La FK artefacto_evaluado_id
//  ya fue resuelta por el service usando el mapa de ids generados en el
//  paso anterior.
//
//  AddRange + SaveChanges único: una sola ida a BD por lote. Si el lote
//  está vacío, EF Core no emite SQL (es seguro llamarlo igual).
//
//  Se asume ejecución dentro de la transacción del IUnitOfWork.
// ============================================================================

using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

public sealed class HallazgoRepository : IHallazgoRepository
{
    private readonly ISOAuditAgentDbContext _context;

    public HallazgoRepository(ISOAuditAgentDbContext context)
    {
        _context = context;
    }

    public async Task AgregarRangoAsync(IEnumerable<Hallazgo> hallazgos, CancellationToken ct)
    {
        _context.Hallazgos.AddRange(hallazgos);
        await _context.SaveChangesAsync(ct);
    }
}
