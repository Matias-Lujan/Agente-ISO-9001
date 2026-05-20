using ISOAuditAgent.Infrastructure.Data.Seed;
using ISOAuditAgent.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.Infrastructure.Data;

/// <summary>
/// <see cref="DbContext"/> de la capa de persistencia del agente.
/// Cubre las entidades necesarias para que DocumentAnalysis pueda
/// resolver artefactos esperados por procedimiento y etapa, mapeos de
/// Drive del proyecto y configuración global del sistema.
/// </summary>
/// <remarks>
/// <para>
/// Alcance de la Fase B (plan de desarrollo del agente): solo las cinco
/// entidades necesarias para que el agente arme su <c>DocumentosExtraidos</c>.
/// Las demás entidades del modelo (<c>Auditoria</c>, <c>Hallazgo</c>,
/// <c>DocumentoAnalizado</c>, etc.) se agregarán en fases posteriores
/// del proyecto.
/// </para>
/// <para>
/// Las configuraciones por entidad viven en
/// <c>Infrastructure/Data/Configurations/</c> y se aplican via
/// <see cref="ModelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly, System.Func{Type, bool}?)"/>.
/// El seed estático (PR 11-13 mínimo + ConfiguracionSistema) vive en
/// <see cref="SeedData.Apply(ModelBuilder)"/>.
/// </para>
/// </remarks>
public sealed class ISOAuditAgentDbContext : DbContext
{
    public ISOAuditAgentDbContext(DbContextOptions<ISOAuditAgentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Procedimiento> Procedimientos => Set<Procedimiento>();
    public DbSet<Etapa> Etapas => Set<Etapa>();
    public DbSet<ArtefactoEsperado> ArtefactosEsperados => Set<ArtefactoEsperado>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<ConfiguracionSistema> ConfiguracionesSistema => Set<ConfiguracionSistema>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ISOAuditAgentDbContext).Assembly);

        SeedData.Apply(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }
}
