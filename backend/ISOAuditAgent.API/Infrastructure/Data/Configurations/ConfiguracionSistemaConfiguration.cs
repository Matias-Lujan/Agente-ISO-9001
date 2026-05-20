using ISOAuditAgent.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ISOAuditAgent.Infrastructure.Data.Configurations;

internal sealed class ConfiguracionSistemaConfiguration
    : IEntityTypeConfiguration<ConfiguracionSistema>
{
    public void Configure(EntityTypeBuilder<ConfiguracionSistema> builder)
    {
        builder.ToTable("configuracion_sistema");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(c => c.Clave)
            .HasColumnName("clave")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Valor)
            .HasColumnName("valor")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(c => c.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(2000);

        builder.HasIndex(c => c.Clave).IsUnique();
    }
}
