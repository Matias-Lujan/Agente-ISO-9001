using ISOAuditAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.Infrastructure.Tests;

/// <summary>
/// Helper compartido para los tests de Infrastructure: construye un
/// <see cref="ISOAuditAgentDbContext"/> sobre el provider InMemory de
/// EF Core, con base de datos única por test (nombre aleatorio) para
/// que los tests no se contaminen entre sí.
/// </summary>
/// <remarks>
/// <para>
/// El seed estático declarado vía <c>HasData</c> en <c>OnModelCreating</c>
/// se aplica automáticamente con el provider InMemory tras llamar a
/// <c>EnsureCreated()</c>.
/// </para>
/// <para>
/// <b>Limitaciones conocidas</b>: InMemory no implementa enteramente la
/// semántica relacional (sin enforcement real de FKs/uniques, sin
/// secuencias). Es suficiente para validar la lógica de los repos y el
/// shape de las consultas, no para tests de integración contra MySQL.
/// </para>
/// </remarks>
internal static class InMemoryDbContextFactory
{
    public static ISOAuditAgentDbContext CreateWithSeed()
    {
        var options = new DbContextOptionsBuilder<ISOAuditAgentDbContext>()
            .UseInMemoryDatabase(databaseName: $"iso-audit-{Guid.NewGuid()}")
            .Options;

        var db = new ISOAuditAgentDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
