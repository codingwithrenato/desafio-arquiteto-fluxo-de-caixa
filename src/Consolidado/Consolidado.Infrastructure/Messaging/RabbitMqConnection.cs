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
    /// Declara a topologia completa (exchanges + fila durável + DLQ) a partir do shared kernel,
    /// com os MESMOS argumentos usados pelo publisher (fonte única de verdade).
    /// </summary>
    private static void DeclareTopology(IModel channel) => RabbitMqTopology.Declare(channel);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection?.Dispose();
    }
}
