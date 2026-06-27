using Consolidado.Application.Abstractions;
using Consolidado.Domain;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.Infrastructure.Persistence;

public sealed class SaldoDiarioRepository(ConsolidadoDbContext context) : ISaldoDiarioRepository
{
    public Task<SaldoDiario?> GetAsync(string comercianteId, DateOnly data, CancellationToken cancellationToken = default)
        => context.SaldosDiarios
            .FirstOrDefaultAsync(x => x.ComercianteId == comercianteId && x.Data == data, cancellationToken);

    public async Task AddAsync(SaldoDiario saldo, CancellationToken cancellationToken = default)
        => await context.SaldosDiarios.AddAsync(saldo, cancellationToken);

    public async Task<IReadOnlyList<SaldoDiario>> GetPorDataAsync(DateOnly data, CancellationToken cancellationToken = default)
        => await context.SaldosDiarios
            .Where(x => x.Data == data)
            .ToListAsync(cancellationToken);
}
