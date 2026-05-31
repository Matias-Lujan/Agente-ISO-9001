// ============================================================================
//  DocumentoAnalizadoRepository — Inserción de lote de DocumentoAnalizado
// ----------------------------------------------------------------------------
//  Implementa IDocumentoAnalizadoRepository. Consumido por
//  AuditoriaPersistenceService como tercer paso del flujo de persistencia.
//  La FK artefacto_evaluado_id ya fue resuelta por el service usando el mapa.
//
//  Mismo patrón que HallazgoRepository: AddRange + SaveChanges único.
//  Se asume ejecución dentro de la transacción del IUnitOfWork.
// ============================================================================

using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

public sealed class DocumentoAnalizadoRepository : IDocumentoAnalizadoRepository
{
    private readonly ISOAuditAgentDbContext _context;

    public DocumentoAnalizadoRepository(ISOAuditAgentDbContext context)
    {
        _context = context;
    }

    public async Task AgregarRangoAsync(
        IEnumerable<DocumentoAnalizado> documentos, CancellationToken ct)
    {
        _context.DocumentosAnalizados.AddRange(documentos);
        await _context.SaveChangesAsync(ct);
    }
}
