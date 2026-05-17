using ISOAuditAgent.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ISOAuditAgent.Infrastructure.Data.Configurations;

internal sealed class EtapaConfiguration : IEntityTypeConfiguration<Etapa>
{
    public void Configure(EntityTypeBuilder<Etapa> builder)
    {
        builder.ToTable("etapa");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(e => e.ProcedimientoId).HasColumnName("procedimiento_id").IsRequired();

        builder.Property(e => e.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Orden).HasColumnName("orden").IsRequired();

        builder.Property(e => e.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(1000);

        builder.HasIndex(e => new { e.ProcedimientoId, e.Orden }).IsUnique();

        builder.HasMany(e => e.ArtefactosEsperados)
            .WithOne(a => a.Etapa)
            .HasForeignKey(a => a.EtapaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
