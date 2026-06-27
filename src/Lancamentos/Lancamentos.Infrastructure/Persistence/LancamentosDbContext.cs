using Lancamentos.Domain;
using Lancamentos.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Persistence;

/// <summary>
/// DbContext do serviço de Lançamentos (database-per-service). Contém o aggregate
/// <see cref="Lancamento"/> e a tabela de <see cref="OutboxMessage"/>, permitindo
/// gravar dado de negócio + evento na mesma transação.
/// </summary>
public sealed class LancamentosDbContext(DbContextOptions<LancamentosDbContext> options) : DbContext(options)
{
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LancamentosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
