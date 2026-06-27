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
}
