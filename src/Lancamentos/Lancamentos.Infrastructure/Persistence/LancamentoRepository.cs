using Lancamentos.Application.Abstractions;
using Lancamentos.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.Infrastructure.Persistence;

public sealed class LancamentoRepository(LancamentosDbContext context) : ILancamentoRepository
{
    public async Task AddAsync(Lancamento lancamento, CancellationToken cancellationToken = default)
        => await context.Lancamentos.AddAsync(lancamento, cancellationToken);

    public async Task<Lancamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Lancamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Lancamento>> GetPorComercianteEDataAsync(
        string comercianteId, DateOnly data, CancellationToken cancellationToken = default)
        => await context.Lancamentos
            .AsNoTracking()
            .Where(x => x.ComercianteId == comercianteId && x.Data == data)
            .OrderBy(x => x.CriadoEmUtc)
            .ToListAsync(cancellationToken);
}
