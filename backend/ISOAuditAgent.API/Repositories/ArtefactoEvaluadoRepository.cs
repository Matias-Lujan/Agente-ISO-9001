// ============================================================================
//  ArtefactoEvaluadoRepository — Inserción de ArtefactoEvaluado
// ----------------------------------------------------------------------------
//  Implementa IArtefactoEvaluadoRepository. Consumido por
//  AuditoriaPersistenceService como PRIMER paso del flujo de persistencia:
//  genera los ids con los que después se resuelven las FKs de Hallazgo y
//  DocumentoAnalizado.
//
//  SaveChangesAsync EXPLÍCITO en cada llamada: el id auto-generado por MySQL
//  se materializa recién después del SaveChanges. El service necesita el id
//  para armar el mapa ArtefactoEsperadoId -> ArtefactoEvaluadoId que usa
//  para resolver las FKs de los pasos siguientes.
//
//  Se asume ejecución DENTRO de la transacción del IUnitOfWork. El UoW
//  agrupa todos estos SaveChanges intermedios en un commit único.
// ============================================================================

using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

public sealed class ArtefactoEvaluadoRepository : IArtefactoEvaluadoRepository
{
    private readonly ISOAuditAgentDbContext _context;

    public ArtefactoEvaluadoRepository(ISOAuditAgentDbContext context)
    {
        _context = context;
    }

    public async Task<int> AgregarAsync(ArtefactoEvaluado artefactoEvaluado, CancellationToken ct)
    {
        _context.ArtefactosEvaluados.Add(artefactoEvaluado);
        await _context.SaveChangesAsync(ct);
        return artefactoEvaluado.Id;
    }
}
