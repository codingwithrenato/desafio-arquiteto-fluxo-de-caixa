using Lancamentos.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lancamentos.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tipo).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RoutingKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Conteudo).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredOnUtc).IsRequired();
        builder.Property(x => x.TraceParent).HasMaxLength(200);
        builder.Property(x => x.UltimoErro).HasMaxLength(2000);

        // Índice parcial: o dispatcher só varre mensagens ainda não processadas.
        builder.HasIndex(x => x.ProcessadoEmUtc)
            .HasFilter("processado_em_utc IS NULL")
            .HasDatabaseName("ix_outbox_pendentes");
    }
}
