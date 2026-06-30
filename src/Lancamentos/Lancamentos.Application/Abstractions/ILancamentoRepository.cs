using Lancamentos.Domain;

namespace Lancamentos.Application.Abstractions;

/// <summary>
/// Porta de persistência do aggregate <see cref="Lancamento"/>.
/// Implementada pela Infraestrutura (padrão Repository + DIP).
/// </summary>
public interface ILancamentoRepository
{
    Task AddAsync(Lancamento lancamento, CancellationToken cancellationToken = default);
    Task<Lancamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lista os lançamentos de um comerciante em um dia (extrato), ordenados por criação.</summary>
    Task<IReadOnlyList<Lancamento>> GetPorComercianteEDataAsync(
        string comercianteId, DateOnly data, CancellationToken cancellationToken = default);
}
