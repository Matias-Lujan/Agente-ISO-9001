using ISOAuditAgent.DocumentAnalysis.Agent;
using ISOAuditAgent.Infrastructure.Data;
using ISOAuditAgent.Infrastructure.DocumentAnalysis;
using ISOAuditAgent.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ISOAuditAgent.Infrastructure.Extensions;

/// <summary>
/// Registro del módulo Infrastructure en el contenedor de DI del host.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Nombre de la cadena de conexión que busca el módulo en
    /// <see cref="IConfiguration"/>.
    /// </summary>
    public const string ConnectionStringName = "Default";

    /// <summary>
    /// Registra el <see cref="ISOAuditAgentDbContext"/> con Pomelo MySQL
    /// y los repositorios de lectura del agente.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lee la cadena de conexión <c>ConnectionStrings:Default</c> de
    /// <paramref name="configuration"/>. Si falta o está vacía, se usa
    /// un placeholder reconocible que falla al ejecutar consultas con un
    /// mensaje claro. La app igual arranca sin BD configurada (la
    /// migración puede no estar aplicada todavía — ver §3.2 del plan de
    /// desarrollo, Fase B iteración 1).
    /// </para>
    /// <para>
    /// El servidor de MySQL se autodetecta con
    /// <c>ServerVersion.AutoDetect</c> en runtime. Para builds /
    /// herramientas EF (migraciones) que necesitan resolver el
    /// <c>ServerVersion</c> sin red, se usa una versión fija de
    /// fallback.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        services.AddDbContext<ISOAuditAgentDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Placeholder reconocible: si alguien intenta usar la BD
                // sin configurarla, el error apunta directo al settings.
                // No usamos InMemory aquí para no esconder problemas en runtime.
                options.UseMySql(
                    "Server=localhost;Database=isoauditagent_unconfigured;User=root;Password=;",
                    ServerVersion.Create(new Version(8, 0, 0), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql));
                return;
            }

            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        });

        services.AddScoped<IProyectoRepository, ProyectoRepository>();
        services.AddScoped<IEtapaRepository, EtapaRepository>();
        services.AddScoped<IArtefactoEsperadoRepository, ArtefactoEsperadoRepository>();
        services.AddScoped<IConfiguracionSistemaRepository, ConfiguracionSistemaRepository>();
        services.AddScoped<IContextoAuditoriaService, ContextoAuditoriaService>();

        return services;
    }
}
