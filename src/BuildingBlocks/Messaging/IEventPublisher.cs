namespace BuildingBlocks.Messaging;

/// <summary>
/// Abstração de publicação de eventos de integração. Isola o domínio/aplicação
/// da tecnologia de mensageria concreta (RabbitMQ), respeitando o DIP (SOLID).
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
