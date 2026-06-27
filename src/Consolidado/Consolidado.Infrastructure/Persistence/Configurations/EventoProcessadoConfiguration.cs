using Consolidado.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Consolidado.Infrastructure.Persistence.Configurations;

public sealed class EventoProcessadoConfiguration : IEntityTypeConfiguration<EventoProcessado>
{
    public void Configure(EntityTypeBuilder<EventoProcessado> builder)
    {
        builder.ToTable("eventos_processados");
        builder.HasKey(x => x.EventId);
        builder.Property(x => x.EventId).ValueGeneratedNever();
        builder.Property(x => x.ProcessadoEmUtc).IsRequired();
    }
}
