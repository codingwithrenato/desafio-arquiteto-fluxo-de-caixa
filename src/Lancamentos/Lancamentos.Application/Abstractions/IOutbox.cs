using BuildingBlocks.Messaging;

namespace Lancamentos.Application.Abstractions;

/// <summary>
/// Porta do padrão Outbox. O caso de uso enfileira o evento de integração
/// que será gravado na MESMA transação do dado de negócio. Um processo de
/// background (dispatcher) publica os eventos pendentes no broker depois.
/// Isso elimina o problema do "dual write" (gravar no banco e publicar no
/// broker de forma não-atômica).
/// </summary>
public interface IOutbox
{
    void Enqueue<TEvent>(TEvent integrationEvent) where TEvent : IIntegrationEvent;
}
