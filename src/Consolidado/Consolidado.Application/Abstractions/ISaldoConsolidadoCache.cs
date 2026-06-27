using Consolidado.Application.Consolidados;

namespace Consolidado.Application.Abstractions;

/// <summary>
/// Cache distribuído (Redis) do saldo consolidado. É o que permite atender 50 req/s
/// de leitura com baixa latência, sem tocar o banco a cada requisição.
/// </summary>
public interface ISaldoConsolidadoCache
{
    Task<SaldoConsolidadoDto?> GetAsync(string comercianteId, DateOnly data, CancellationToken cancellationToken = default);
    Task SetAsync(SaldoConsolidadoDto saldo, CancellationToken cancellationToken = default);
    Task RemoveAsync(string comercianteId, DateOnly data, CancellationToken cancellationToken = default);
}
