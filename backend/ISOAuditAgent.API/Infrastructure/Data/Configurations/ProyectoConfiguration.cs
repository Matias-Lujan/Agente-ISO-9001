using ISOAuditAgent.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ISOAuditAgent.Infrastructure.Data.Configurations;

internal sealed class ProyectoConfiguration : IEntityTypeConfiguration<Proyecto>
{
    public void Configure(EntityTypeBuilder<Proyecto> builder)
    {
        builder.ToTable("proyecto");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(p => p.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(2000);

        builder.Property(p => p.FechaInicio).HasColumnName("fecha_inicio");
        builder.Property(p => p.FechaFin).HasColumnName("fecha_fin");

        // El enum TipoProyecto se persiste como string ('A' / 'B') por
        // claridad operativa al inspeccionar la BD.
        builder.Property(p => p.TipoProyecto)
            .HasColumnName("tipo_proyecto")
            .HasConversion<string>()
            .HasMaxLength(1)
            .IsRequired();

        builder.Property(p => p.HorasEstimadas)
            .HasColumnName("horas_estimadas")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.ProcedimientoId).HasColumnName("procedimiento_id").IsRequired();

        builder.Property(p => p.TrelloBoardId)
            .HasColumnName("trello_board_id")
            .HasMaxLength(100);

        builder.Property(p => p.ClockifyProjectId)
            .HasColumnName("clockify_project_id")
            .HasMaxLength(100);

        builder.Property(p => p.DriveFolderId)
            .HasColumnName("drive_folder_id")
            .HasMaxLength(100);

        builder.Property(p => p.Activo)
            .HasColumnName("activo")
            .IsRequired();

        builder.HasOne(p => p.Procedimiento)
            .WithMany()
            .HasForeignKey(p => p.ProcedimientoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
