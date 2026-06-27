using Lancamentos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lancamentos.Infrastructure.Persistence.Configurations;

public sealed class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ComercianteId)
            .HasMaxLength(100)
            .IsRequired();

        // Value Object Money mapeado para uma coluna decimal(18,2).
        builder.Property(x => x.Valor)
            .HasConversion(v => v.Amount, v => Money.From(v))
            .HasColumnName("valor")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.Tipo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Data)
            .IsRequired();

        builder.Property(x => x.CriadoEmUtc)
            .IsRequired();

        builder.Property(x => x.Descricao)
            .HasMaxLength(250);

        // Índice para consultas por comerciante e dia.
        builder.HasIndex(x => new { x.ComercianteId, x.Data });
    }
}
