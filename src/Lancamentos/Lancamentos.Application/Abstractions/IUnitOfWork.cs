namespace Lancamentos.Application.Abstractions;

/// <summary>
/// Unit of Work: confirma, em uma única transação, a gravação do lançamento
/// e da mensagem de outbox. É o que garante a atomicidade do padrão Outbox.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
