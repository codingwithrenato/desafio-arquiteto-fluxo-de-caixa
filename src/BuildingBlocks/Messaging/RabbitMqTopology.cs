using RabbitMQ.Client;

namespace BuildingBlocks.Messaging;

/// <summary>
/// Declaração única e idempotente da topologia de mensageria, compartilhada entre
/// publisher (Lançamentos) e consumer (Consolidado). Centralizar aqui garante:
/// - que os argumentos da fila sejam IDÊNTICOS nos dois lados (evita PRECONDITION_FAILED);
/// - que a fila durável exista assim que QUALQUER serviço conectar — então mensagens
///   publicadas numa subida fria não se perdem mesmo se o consumidor ainda não subiu.
/// </summary>
public static class RabbitMqTopology
{
    public static void Declare(IModel channel)
    {
        // Exchange principal (topic, durável) + dead-letter exchange.
        channel.ExchangeDeclare(MessagingTopology.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(MessagingTopology.DeadLetterExchange, ExchangeType.Topic, durable: true, autoDelete: false);

        // Fila do consumidor com dead-lettering, ligada ao exchange pela routing key.
        var args = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = MessagingTopology.DeadLetterExchange,
            ["x-dead-letter-routing-key"] = MessagingTopology.LancamentoRegistradoRoutingKey
        };
        channel.QueueDeclare(MessagingTopology.ConsolidadoQueue, durable: true, exclusive: false, autoDelete: false, arguments: args);
        channel.QueueBind(MessagingTopology.ConsolidadoQueue, MessagingTopology.Exchange, MessagingTopology.LancamentoRegistradoRoutingKey);

        // Dead-letter queue.
        channel.QueueDeclare(MessagingTopology.ConsolidadoDeadLetterQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(MessagingTopology.ConsolidadoDeadLetterQueue, MessagingTopology.DeadLetterExchange, MessagingTopology.LancamentoRegistradoRoutingKey);
    }
}
