// ============================================================================
//  ProcedimientoRepository — Etapas y artefactos esperados del procedimiento
// ----------------------------------------------------------------------------
//  Implementa IProcedimientoRepository. Consumido por ResolutorContextoService
//  para obtener la estructura del procedimiento que rige al proyecto auditado.
//
//  INVARIANTE CRÍTICO (riesgo 4 del plan maestro §6):
//  ObtenerArtefactosEsperadosAsync DEBE hacer Include(ae => ae.Etapa).
//  ResolutorContextoService accede a ae.Etapa.Orden para calcular
//  exigibilidad. Sin el Include, NullReferenceException en runtime
//  (no en compilación) — EF Core no hace lazy loading por defecto.
// ============================================================================

using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

public sealed class ProcedimientoRepository : IProcedimientoRepository
{
    private readonly ISOAuditAgentDbContext _context;

    public ProcedimientoRepository(ISOAuditAgentDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Etapa>> ObtenerEtapasAsync(
        int procedimientoId, CancellationToken ct)
    {
        return await _context.Etapas
            .AsNoTracking()
            .Where(e => e.ProcedimientoId == procedimientoId)
            .OrderBy(e => e.Orden)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ArtefactoEsperado>> ObtenerArtefactosEsperadosAsync(
        int procedimientoId, CancellationToken ct)
    {
        // Include obligatorio: ResolutorContextoService usa ae.Etapa.Orden.
        // El filtro navega vía Etapa.ProcedimientoId — ArtefactoEsperado no
        // tiene FK directa al procedimiento.
        return await _context.ArtefactosEsperados
            .AsNoTracking()
            .Include(ae => ae.Etapa)
            .Where(ae => ae.Etapa.ProcedimientoId == procedimientoId)
            .ToListAsync(ct);
    }
}
