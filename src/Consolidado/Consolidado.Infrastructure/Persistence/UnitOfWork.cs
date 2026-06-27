using Consolidado.Application.Abstractions;

namespace Consolidado.Infrastructure.Persistence;

public sealed class UnitOfWork(ConsolidadoDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
