using Consolidado.Domain;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.Infrastructure.Persistence;

/// <summary>
/// DbContext do serviço de Consolidado (database-per-service). Guarda a projeção
/// <see cref="SaldoDiario"/> e o registro de idempotência <see cref="EventoProcessado"/>.
/// </summary>
public sealed class ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options) : DbContext(options)
{
    public DbSet<SaldoDiario> SaldosDiarios => Set<SaldoDiario>();
    public DbSet<EventoProcessado> EventosProcessados => Set<EventoProcessado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConsolidadoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
