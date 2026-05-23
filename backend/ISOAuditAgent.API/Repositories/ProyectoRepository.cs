// ============================================================================
//  ProyectoRepository — Acceso de lectura a la entidad Proyecto
// ----------------------------------------------------------------------------
//  Implementa IProyectoRepository. Consumido por ResolutorContextoService
//  para obtener el proyecto auditado (tipo, procedimiento, integraciones
//  Drive/Trello/Clockify) al inicio del workflow.
//
//  No filtra por Activo: la decisión de auditar o no un proyecto inactivo es
//  del endpoint REST (D6), no del repositorio. El repo expone el dato como
//  está en la BD.
// ============================================================================

using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

public sealed class ProyectoRepository : IProyectoRepository
{
    private readonly ISOAuditAgentDbContext _context;

    public ProyectoRepository(ISOAuditAgentDbContext context)
    {
        _context = context;
    }

    public Task<Proyecto?> ObtenerPorIdOrNullAsync(int proyectoId, CancellationToken ct)
    {
        return _context.Proyectos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == proyectoId, ct);
    }
}
