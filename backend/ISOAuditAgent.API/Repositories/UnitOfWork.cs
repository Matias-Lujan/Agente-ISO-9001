// ============================================================================
//  UnitOfWork — Transacción EF Core sobre ISOAuditAgentDbContext
// ----------------------------------------------------------------------------
//  Implementa IUnitOfWork. La usa AuditoriaPersistenceService para envolver
//  el flujo de persistencia (insertar ArtefactoEvaluado + Hallazgo +
//  DocumentoAnalizado + cambio de estado de Auditoria) en una sola unidad
//  atómica.
//
//  Por qué un UnitOfWork explícito: el flujo de persistencia hace SaveChanges
//  INTERMEDIOS para materializar los ids autogenerados de ArtefactoEvaluado
//  (necesarios para resolver las FKs de Hallazgo y DocumentoAnalizado). Sin
//  una transacción explícita, esos SaveChanges intermedios ya estarían
//  commiteados y no habría forma de revertirlos si el siguiente paso falla.
//  La transacción agrupa todos los SaveChanges en un commit único — o en un
//  rollback único.
//
//  La operación que se le pasa al UoW SE ASUME que llama a SaveChangesAsync
//  por sí misma (los repositorios concretos lo hacen). El UoW solo aporta el
//  Begin/Commit/Rollback.
// ============================================================================

using ISOAuditAgent.API.Data;

namespace ISOAuditAgent.API.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ISOAuditAgentDbContext _context;

    public UnitOfWork(ISOAuditAgentDbContext context)
    {
        _context = context;
    }

    public async Task EjecutarEnTransaccionAsync(
        Func<CancellationToken, Task> operacion, CancellationToken ct)
    {
        // Abrimos la transacción explícita sobre la conexión del DbContext.
        // Pomelo.EntityFrameworkCore.MySql la soporta de forma nativa.
        await using var tx = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            await operacion(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            // Rollback explícito y propagar. El catch del worker
            // (AuditoriaRunner) traduce la excepción a estado Fallida.
            // Usamos CancellationToken.None en el rollback: si la operación
            // se canceló, igual queremos garantizar el rollback antes de
            // propagar.
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
