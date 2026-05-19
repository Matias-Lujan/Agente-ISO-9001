using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ISOAuditAgent.Infrastructure.Data;

/// <summary>
/// Factory que las herramientas de EF Core (<c>dotnet ef migrations add</c>,
/// <c>dotnet ef database update</c>, etc.) descubren por reflexión para
/// construir un <see cref="ISOAuditAgentDbContext"/> sin levantar la
/// aplicación host.
/// </summary>
/// <remarks>
/// <para>
/// Usa una <c>ServerVersion</c> fija (MySQL 8.0) en lugar de
/// <c>AutoDetect</c> porque las herramientas de design-time pueden no
/// tener un servidor disponible. La cadena de conexión es un placeholder
/// — no se conecta a la BD durante <c>migrations add</c>; solo se necesita
/// para que el proveedor (Pomelo) sepa qué SQL generar.
/// </para>
/// <para>
/// Si en el futuro se quiere correr migraciones contra una BD real desde
/// CLI, se puede leer una variable de entorno
/// (<c>ISOAUDITAGENT_CONNECTION_STRING</c>) en lugar del placeholder.
/// </para>
/// </remarks>
internal sealed class DesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<ISOAuditAgentDbContext>
{
    public ISOAuditAgentDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ISOAuditAgentDbContext>();

        builder.UseMySql(
            "Server=localhost;Database=isoauditagent;User=root;Password=;",
            new MySqlServerVersion(new Version(8, 0, 0)));

        return new ISOAuditAgentDbContext(builder.Options);
    }
}
