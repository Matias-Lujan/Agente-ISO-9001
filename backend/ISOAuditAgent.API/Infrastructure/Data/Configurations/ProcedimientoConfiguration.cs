using ISOAuditAgent.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ISOAuditAgent.Infrastructure.Data.Configurations;

internal sealed class ProcedimientoConfiguration : IEntityTypeConfiguration<Procedimiento>
{
    public void Configure(EntityTypeBuilder<Procedimiento> builder)
    {
        builder.ToTable("procedimiento");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(p => p.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(1000);

        builder.HasIndex(p => p.Codigo).IsUnique();

        builder.HasMany(p => p.Etapas)
            .WithOne(e => e.Procedimiento)
            .HasForeignKey(e => e.ProcedimientoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
