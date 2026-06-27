using Consolidado.Domain;

namespace Consolidado.Application.Abstractions;

public interface ISaldoDiarioRepository
{
    Task<SaldoDiario?> GetAsync(string comercianteId, DateOnly data, CancellationToken cancellationToken = default);
    Task AddAsync(SaldoDiario saldo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SaldoDiario>> GetPorDataAsync(DateOnly data, CancellationToken cancellationToken = default);
}
