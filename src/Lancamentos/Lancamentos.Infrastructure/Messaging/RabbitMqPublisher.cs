using System.Text.Json;
using BuildingBlocks.Messaging;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RabbitMQ.Client;

namespace Lancamentos.Infrastructure.Messaging;

/// <summary>
/// Publisher RabbitMQ. Implementa tanto a porta tipada <see cref="IEventPublisher"/>
/// (publicação direta de um evento) quanto a publicação de baixo nível
/// <see cref="IMessageBusPublisher"/> usada pelo OutboxDispatcher.
///
/// Resiliência (Polly): retry exponencial + circuit breaker em torno do publish.
/// Confiabilidade: mensagens persistentes + publisher confirms (WaitForConfirmsOrDie).
/// </summary>
public sealed class RabbitMqPublisher : IEventPublisher, IMessageBusPublisher
{
    private readonly IRabbitMqConnection _connection;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly ResiliencePipeline _resilience;

    public RabbitMqPublisher(IRabbitMqConnection connection, ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
        _resilience = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 8,
                BreakDuration = TimeSpan.FromSeconds(15)
            })
            .Build();
    }

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(
            integrationEvent, integrationEvent.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // A routing key da publicação direta usa o nome do tipo por convenção.
        Publish(typeof(TEvent).Name, body, integrationEvent.EventId.ToString());
        return Task.CompletedTask;
    }

    public void Publish(string routingKey, ReadOnlyMemory<byte> body, string messageId,
        IDictionary<string, object>? headers = null)
    {
        _resilience.Execute(() =>
        {
            using var channel = _connection.CreateChannel();
            channel.ConfirmSelect();

            var props = channel.CreateBasicProperties();
            props.Persistent = true;              // sobrevive a restart do broker
            props.MessageId = messageId;
            props.ContentType = "application/json";
            props.DeliveryMode = 2;
            if (headers is { Count: > 0 })
                props.Headers = headers;          // contexto de trace (propagação)

            channel.BasicPublish(
                exchange: MessagingTopology.Exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: props,
                body: body);

            // Bloqueia até o broker confirmar a persistência (at-least-once garantido).
            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

            _logger.LogDebug("Mensagem {MessageId} publicada em {RoutingKey}.", messageId, routingKey);
        });
    }
}
