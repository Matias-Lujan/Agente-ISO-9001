using Microsoft.EntityFrameworkCore;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Data;

public class ISOAuditAgentDbContext : DbContext
{
    public ISOAuditAgentDbContext(DbContextOptions<ISOAuditAgentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Procedimiento> Procedimientos => Set<Procedimiento>();
    public DbSet<Etapa> Etapas => Set<Etapa>();
    public DbSet<ArtefactoEsperado> ArtefactosEsperados => Set<ArtefactoEsperado>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<ProyectoUsuario> ProyectoUsuarios => Set<ProyectoUsuario>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<ArtefactoEvaluado> ArtefactosEvaluados => Set<ArtefactoEvaluado>();
    public DbSet<Hallazgo> Hallazgos => Set<Hallazgo>();
    public DbSet<DocumentoAnalizado> DocumentosAnalizados => Set<DocumentoAnalizado>();
    public DbSet<Informe> Informes => Set<Informe>();
    public DbSet<ConfiguracionSistema> ConfiguracionesSistema => Set<ConfiguracionSistema>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // === Convención global: snake_case para tablas y columnas ===
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName()!));

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }
        }

        // === Enums guardados como texto (en lugar de número) ===
        modelBuilder.Entity<Usuario>()
            .Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Proyecto>()
            .Property(p => p.TipoProyecto).HasConversion<string>().HasMaxLength(1);
        modelBuilder.Entity<Auditoria>()
            .Property(a => a.Estado).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<ArtefactoEvaluado>()
            .Property(ae => ae.Aplica).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<Hallazgo>()
            .Property(h => h.Tipo).HasConversion<string>().HasMaxLength(10);
        modelBuilder.Entity<Hallazgo>()
            .Property(h => h.AgenteOrigen).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<DocumentoAnalizado>()
            .Property(d => d.Fuente).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Informe>()
            .Property(i => i.Tipo).HasConversion<string>().HasMaxLength(10);

        // === Índices únicos ===
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Procedimiento>()
            .HasIndex(p => p.Codigo).IsUnique();
        modelBuilder.Entity<ConfiguracionSistema>()
            .HasIndex(c => c.Clave).IsUnique();
        modelBuilder.Entity<ProyectoUsuario>()
            .HasIndex(pu => new { pu.ProyectoId, pu.UsuarioId }).IsUnique();

        // === Seed de configuración del sistema ===
        // path_carpeta_templates: carpeta donde viven los templates de los
        // artefactos. ResolutorContextoService la lee (vía IConfiguracionRepository)
        // y la concatena con ArtefactoEsperado.PathTemplateRelativo para resolver
        // la ruta absoluta de cada template. Si esta clave falta, el workflow
        // falla temprano con ContextoAuditoriaException.
        //
        // Valor "./templates": default de desarrollo, ruta relativa al working
        // directory de la API. Es CONFIGURABLE — al ser una fila de una tabla
        // key-value, se reemplaza por la ruta real de cada entorno sin tocar
        // código ni recompilar.
        modelBuilder.Entity<ConfiguracionSistema>().HasData(
            new ConfiguracionSistema
            {
                Id = 1,
                Clave = "path_carpeta_templates",
                Valor = "./templates",
                Descripcion = "Ruta de la carpeta de templates de artefactos. "
                            + "Configurable por entorno: reemplazar el valor por "
                            + "la ruta real donde el deployment aloja los templates."
            });

        // === Comportamiento de borrado: Restrict en todas las FKs ===
        // Evita el borrado en cascada por defecto de EF Core.
        // Coherente con la trazabilidad (RNF-05) y con el uso del campo
        // 'activo' para deshabilitar en lugar de borrar.
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    // Helper para convertir PascalCase a snake_case.
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

}