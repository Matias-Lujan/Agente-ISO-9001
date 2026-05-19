using ISOAuditAgent.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ISOAuditAgent.Infrastructure.Data.Configurations;

internal sealed class ArtefactoEsperadoConfiguration : IEntityTypeConfiguration<ArtefactoEsperado>
{
    public void Configure(EntityTypeBuilder<ArtefactoEsperado> builder)
    {
        builder.ToTable("artefacto_esperado");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(a => a.EtapaId).HasColumnName("etapa_id").IsRequired();

        builder.Property(a => a.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(50);

        builder.Property(a => a.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(2000);

        builder.Property(a => a.MandatorioTipoA)
            .HasColumnName("mandatorio_tipo_a")
            .IsRequired();

        builder.Property(a => a.MandatorioTipoB)
            .HasColumnName("mandatorio_tipo_b")
            .IsRequired();

        builder.Property(a => a.TemplateDriveFilename)
            .HasColumnName("template_drive_filename")
            .HasMaxLength(255);

        builder.HasIndex(a => new { a.EtapaId, a.Codigo });
    }
}
