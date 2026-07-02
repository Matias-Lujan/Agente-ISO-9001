using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Data;

/// <summary>
/// DbContext principal de la aplicacion.
/// Es el puente entre el codigo C# y la base de datos MySQL.
/// Define todas las tablas y sus configuraciones.
/// </summary>
public class ISOAuditAgentDbContext : DbContext
{
    public ISOAuditAgentDbContext(DbContextOptions<ISOAuditAgentDbContext> options)
        : base(options) { }

    // ── Tablas ────────────────────────────────────────────────────────────────
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<ProyectoUsuario> ProyectosUsuarios => Set<ProyectoUsuario>();
    public DbSet<Procedimiento> Procedimientos => Set<Procedimiento>();
    public DbSet<Etapa> Etapas => Set<Etapa>();
    public DbSet<ArtefactoEsperado> ArtefactosEsperados => Set<ArtefactoEsperado>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<ArtefactoEvaluado> ArtefactosEvaluados => Set<ArtefactoEvaluado>();
    public DbSet<Hallazgo> Hallazgos => Set<Hallazgo>();
    public DbSet<AuditoriaProgreso> AuditoriaProgresos => Set<AuditoriaProgreso>();
    public DbSet<RegistroErrorAuditoria> RegistrosErrorAuditoria => Set<RegistroErrorAuditoria>();
    public DbSet<DocumentoAnalizado> DocumentosAnalizados => Set<DocumentoAnalizado>();
    public DbSet<Informe> Informes => Set<Informe>();
    public DbSet<ConfiguracionSistema> ConfiguracionesSistema => Set<ConfiguracionSistema>();
    public DbSet<FormularioCalidad> FormulariosCalidad => Set<FormularioCalidad>();
    public DbSet<PromptAgente> PromptsAgente => Set<PromptAgente>();
    public DbSet<ConsumoTokens> ConsumosTokens => Set<ConsumoTokens>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Enums como texto en la BD ─────────────────────────────────────────
        // En lugar de guardar 0, 1, 2 guardamos "Administrador", "Auditor", etc.
        // Esto hace la BD legible por humanos
        modelBuilder.Entity<Usuario>()
            .Property(u => u.Rol)
            .HasConversion<string>();

        // Preferencia de tema (claro/oscuro) guardada como texto. Default Claro
        // para que las filas existentes y los usuarios nuevos arranquen en claro.
        modelBuilder.Entity<Usuario>()
            .Property(u => u.TemaPreferido)
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(TemaPreferido.Claro);

        modelBuilder.Entity<Proyecto>()
            .Property(p => p.TipoProyecto)
            .HasConversion<string>();

        modelBuilder.Entity<Auditoria>()
            .Property(a => a.Estado)
            .HasConversion<string>();

        // Categoría del fallo guardada como texto legible en la BD (null si no falló).
        modelBuilder.Entity<Auditoria>()
            .Property(a => a.CategoriaError)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<ArtefactoEvaluado>()
            .Property(a => a.Aplica)
            .HasConversion<string>();

        modelBuilder.Entity<ArtefactoEvaluado>()
            .Property(a => a.Resultado)
            .HasConversion<string>();

        modelBuilder.Entity<Hallazgo>()
            .Property(h => h.Tipo)
            .HasConversion<string>();

        modelBuilder.Entity<Hallazgo>()
            .Property(h => h.AgenteOrigen)
            .HasConversion<string>();

        modelBuilder.Entity<DocumentoAnalizado>()
            .Property(d => d.Fuente)
            .HasConversion<string>();

        modelBuilder.Entity<Informe>()
            .Property(i => i.Tipo)
            .HasConversion<string>();

        // Fuente donde se verifica el artefacto (Drive/Trello/Clockify) — de dev (ML-06).
        modelBuilder.Entity<ArtefactoEsperado>()
            .Property(a => a.FuenteVerificacion)
            .HasConversion<string>()
            .HasMaxLength(20);

        // ── Progreso del workflow (portado de dev) ────────────────────────────
        modelBuilder.Entity<AuditoriaProgreso>()
            .Property(p => p.Nodo).HasConversion<string>();
        modelBuilder.Entity<AuditoriaProgreso>()
            .Property(p => p.Estado).HasConversion<string>();
        modelBuilder.Entity<AuditoriaProgreso>()
            .HasIndex(p => new { p.AuditoriaId, p.Nodo }).IsUnique();
        modelBuilder.Entity<AuditoriaProgreso>().ToTable("auditoria_progreso");

        // ── Registro durable de errores de auditoría ──────────────────────────
        modelBuilder.Entity<RegistroErrorAuditoria>().ToTable("registros_error_auditoria");
        modelBuilder.Entity<RegistroErrorAuditoria>()
            .Property(r => r.Nodo).HasConversion<string>().HasMaxLength(40);
        modelBuilder.Entity<RegistroErrorAuditoria>()
            .Property(r => r.Categoria).HasConversion<string>().HasMaxLength(40);
        modelBuilder.Entity<RegistroErrorAuditoria>()
            .HasIndex(r => r.AuditoriaId);
        modelBuilder.Entity<RegistroErrorAuditoria>()
            .HasOne(r => r.Auditoria)
            .WithMany(a => a.RegistrosError)
            .HasForeignKey(r => r.AuditoriaId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Nombres de tablas en plural ───────────────────────────────────────
        modelBuilder.Entity<Usuario>().ToTable("usuarios");
        modelBuilder.Entity<Proyecto>().ToTable("proyectos");
        modelBuilder.Entity<ProyectoUsuario>().ToTable("proyectos_usuarios");
        modelBuilder.Entity<Procedimiento>().ToTable("procedimientos");
        modelBuilder.Entity<Etapa>().ToTable("etapas");
        modelBuilder.Entity<ArtefactoEsperado>().ToTable("artefactos_esperados");
        modelBuilder.Entity<Auditoria>().ToTable("auditorias");
        modelBuilder.Entity<ArtefactoEvaluado>().ToTable("artefactos_evaluados");
        modelBuilder.Entity<Hallazgo>().ToTable("hallazgos");
        modelBuilder.Entity<DocumentoAnalizado>().ToTable("documentos_analizados");
        modelBuilder.Entity<Informe>().ToTable("informes");
        modelBuilder.Entity<ConfiguracionSistema>().ToTable("configuraciones_sistema");

        // ── System prompts de los agentes (versionado append-only) ────────────
        modelBuilder.Entity<PromptAgente>().ToTable("prompts_agente");
        modelBuilder.Entity<PromptAgente>()
            .Property(p => p.AgenteKey)
            .HasMaxLength(60);
        // Una sola versión por (AgenteKey, Version).
        modelBuilder.Entity<PromptAgente>()
            .HasIndex(p => new { p.AgenteKey, p.Version })
            .IsUnique();
        // Búsqueda del prompt en uso: por key + activa.
        modelBuilder.Entity<PromptAgente>()
            .HasIndex(p => new { p.AgenteKey, p.EsActiva });
        modelBuilder.Entity<PromptAgente>()
            .HasOne(p => p.ModificadoPor)
            .WithMany()
            .HasForeignKey(p => p.ModificadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Consumo de tokens del LLM (una fila por llamada) ──────────────────
        // Tabla standalone: AuditoriaId es informativa y sin FK, para conservar
        // el histórico de consumo aunque la auditoría se borre.
        modelBuilder.Entity<ConsumoTokens>().ToTable("consumo_tokens");
        modelBuilder.Entity<ConsumoTokens>()
            .Property(c => c.AgenteKey).HasMaxLength(60);
        modelBuilder.Entity<ConsumoTokens>()
            .Property(c => c.Modelo).HasMaxLength(100);
        modelBuilder.Entity<ConsumoTokens>()
            .HasIndex(c => c.FechaHoraUtc);
        modelBuilder.Entity<ConsumoTokens>()
            .HasIndex(c => c.AgenteKey);

        // Referencia del Departamento de Calidad: vigencia vigente por formulario.
        modelBuilder.Entity<FormularioCalidad>().ToTable("formularios_calidad");
        modelBuilder.Entity<FormularioCalidad>()
            .Property(f => f.CodigoFormulario)
            .HasMaxLength(20);
        modelBuilder.Entity<FormularioCalidad>()
            .HasIndex(f => f.CodigoFormulario)
            .IsUnique();

        // ── Relaciones ────────────────────────────────────────────────────────
        modelBuilder.Entity<ProyectoUsuario>()
            .HasOne(pu => pu.Proyecto)
            .WithMany(p => p.ProyectoUsuarios)
            .HasForeignKey(pu => pu.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProyectoUsuario>()
            .HasOne(pu => pu.Usuario)
            .WithMany(u => u.ProyectoUsuarios)
            .HasForeignKey(pu => pu.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Etapa>()
            .HasOne(e => e.Procedimiento)
            .WithMany(p => p.Etapas)
            .HasForeignKey(e => e.ProcedimientoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ArtefactoEsperado>()
            .HasOne(a => a.Etapa)
            .WithMany(e => e.ArtefactosEsperados)
            .HasForeignKey(a => a.EtapaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Auditoria>()
            .HasOne(a => a.Proyecto)
            .WithMany(p => p.Auditorias)
            .HasForeignKey(a => a.ProyectoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Auditoria>()
            .HasOne(a => a.Usuario)
            .WithMany(u => u.Auditorias)
            .HasForeignKey(a => a.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Auditoria>()
            .HasOne(a => a.Etapa)
            .WithMany(e => e.Auditorias)
            .HasForeignKey(a => a.EtapaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ArtefactoEvaluado>()
            .HasOne(ae => ae.Auditoria)
            .WithMany(a => a.ArtefactosEvaluados)
            .HasForeignKey(ae => ae.AuditoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ArtefactoEvaluado>()
            .HasOne(ae => ae.ArtefactoEsperado)
            .WithMany(a => a.ArtefactosEvaluados)
            .HasForeignKey(ae => ae.ArtefactoEsperadoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Hallazgo>()
            .HasOne(h => h.ArtefactoEvaluado)
            .WithMany(ae => ae.Hallazgos)
            .HasForeignKey(h => h.ArtefactoEvaluadoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DocumentoAnalizado>()
            .HasOne(d => d.Auditoria)
            .WithMany(a => a.DocumentosAnalizados)
            .HasForeignKey(d => d.AuditoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DocumentoAnalizado>()
            .HasOne(d => d.ArtefactoEvaluado)
            .WithMany(ae => ae.DocumentosAnalizados)
            .HasForeignKey(d => d.ArtefactoEvaluadoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Informe>()
            .HasOne(i => i.Auditoria)
            .WithMany(a => a.Informes)
            .HasForeignKey(i => i.AuditoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indices unicos ────────────────────────────────────────────────────
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Procedimiento>()
            .HasIndex(p => p.Codigo)
            .IsUnique();
    }
}