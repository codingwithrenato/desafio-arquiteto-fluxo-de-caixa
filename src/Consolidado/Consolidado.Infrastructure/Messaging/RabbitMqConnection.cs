using BuildingBlocks.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;

namespace Consolidado.Infrastructure.Messaging;

public interface IRabbitMqConnection : IDisposable
{
    IModel CreateChannel();
}

/// <summary>
/// Conexão durável com o RabbitMQ e declaração da topologia do lado consumidor:
/// fila durável ligada ao exchange, com dead-letter exchange/queue para mensagens
/// que falham repetidamente (poison messages).
/// </summary>
public sealed class RabbitMqConnection : IRabbitMqConnection
{
    private readonly IConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly ResiliencePipeline _connectRetry;
    private readonly object _gate = new();
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger)
    {
        _logger = logger;
        var o = options.Value;
        _factory = new ConnectionFactory
        {
            HostName = o.HostName,
            Port = o.Port,
            UserName = o.UserName,
            Password = o.Password,
            VirtualHost = o.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };

        _connectRetry = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning("Falha ao conectar no RabbitMQ (tentativa {Attempt}). Retentando...",
                        args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public IModel CreateChannel()
    {
        EnsureConnected();
        var channel = _connection!.CreateModel();
        DeclareTopology(channel);
        return channel;
    }

    private void EnsureConnected()
    {
        if (_connection is { IsOpen: true }) return;

        lock (_gate)
        {
            if (_connection is { IsOpen: true }) return;
            _connectRetry.Execute(() =>
            {
                _connection?.Dispose();
                _connection = _factory.CreateConnection("consolidado-consumer");
                _logger.LogInformation("Conexão com RabbitMQ estabelecida.");
            });
        }
    }

    /// <summary>
    /// Declara, de forma idempotente, exchange principal + DLX e a fila do consumidor
    /// com a DLQ vinculada. A fila aponta seu dead-letter-exchange para o DLX.
    /// </summary>
    private static void DeclareTopology(IModel channel)
    {
        channel.ExchangeDeclare(MessagingTopology.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        channel.ExchangeDeclare(MessagingTopology.DeadLetterExchange, ExchangeType.Topic, durable: true, autoDelete: false);

        // Fila principal com dead-lettering.
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection?.Dispose();
    }
}
