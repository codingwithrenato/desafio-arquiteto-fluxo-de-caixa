using Consolidado.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Consolidado.Infrastructure.Persistence.Configurations;

public sealed class SaldoDiarioConfiguration : IEntityTypeConfiguration<SaldoDiario>
{
    public void Configure(EntityTypeBuilder<SaldoDiario> builder)
    {
        builder.ToTable("saldos_diarios");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ComercianteId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Data).IsRequired();
        builder.Property(x => x.TotalCreditos).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalDebitos).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.QuantidadeLancamentos).IsRequired();
        builder.Property(x => x.AtualizadoEmUtc).IsRequired();
        builder.Property(x => x.Fechado).IsRequired();

        // Saldo é calculado em memória — não persiste.
        builder.Ignore(x => x.Saldo);

        // Um único consolidado por comerciante por dia.
        builder.HasIndex(x => new { x.ComercianteId, x.Data }).IsUnique();

        // Concorrência otimista via xmin (system column do PostgreSQL): protege a projeção
        // quando há várias réplicas do worker atualizando o mesmo saldo simultaneamente.
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
