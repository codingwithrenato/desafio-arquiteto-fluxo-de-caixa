using Lancamentos.Application.Abstractions;

namespace Lancamentos.Infrastructure.Persistence;

public sealed class UnitOfWork(LancamentosDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
