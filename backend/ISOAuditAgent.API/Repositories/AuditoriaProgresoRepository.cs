// ============================================================================
//  AuditoriaProgresoRepository — Lectura del progreso del workflow
// ----------------------------------------------------------------------------
//  Implementa IAuditoriaProgresoRepository. Lo consume el endpoint
//  GET /api/auditorias/{id}/progreso para alimentar el polling del frontend.
//
//  No tiene métodos de escritura. La escritura del progreso vive en
//  IAuditoriaProgresoTracker, que usa un DbContext aparte (a través de
//  IServiceScopeFactory) para no contaminar la transacción de persistencia
//  del resultado de la auditoría.
//
//  El orden de las filas se calcula a partir del enum NodoWorkflow: la
//  declaración del enum sigue la secuencia natural del workflow
//  (Document → Compliance → Consistency → Findings).
// ============================================================================

using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

public sealed class AuditoriaProgresoRepository : IAuditoriaProgresoRepository
{
    private readonly ISOAuditAgentDbContext _context;

    public AuditoriaProgresoRepository(ISOAuditAgentDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AuditoriaProgreso>> ObtenerPorAuditoriaAsync(
        int auditoriaId, CancellationToken ct)
    {
        return await _context.AuditoriaProgresos
            .AsNoTracking()
            .Where(p => p.AuditoriaId == auditoriaId)
            .OrderBy(p => p.Nodo)
            .ToListAsync(ct);
    }
}
