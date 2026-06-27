using System.Text;
using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using Consolidado.Application.Consolidados.ConsolidarLancamento;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consolidado.Infrastructure.Messaging;

/// <summary>
/// Consumidor da fila de lançamentos. Para cada mensagem, dispara o caso de uso de
/// consolidação (idempotente) via MediatR.
///
/// Política de ack:
/// - Sucesso: ACK.
/// - Erro transitório (ex.: banco momentaneamente indisponível): retry com Polly;
///   se persistir, NACK com requeue para nova tentativa posterior.
/// - Mensagem inválida (não desserializa): NACK sem requeue → vai para a DLQ.
/// </summary>
public sealed class LancamentoRegistradoConsumer(
    IRabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<LancamentoRegistradoConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options = options.Value;
    private IModel? _channel;

    private readonly ResiliencePipeline _retry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(300),
            BackoffType = DelayBackoffType.Exponential
        })
        .Build();

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = connection.CreateChannel();
        _channel.BasicQos(prefetchSize: 0, prefetchCount: _options.PrefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageAsync;

        _channel.BasicConsume(MessagingTopology.ConsolidadoQueue, autoAck: false, consumer);
        logger.LogInformation("Consumidor iniciado na fila {Fila} (prefetch {Prefetch}).",
            MessagingTopology.ConsolidadoQueue, _options.PrefetchCount);

        return Task.CompletedTask;
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        LancamentoRegistradoEvent? evento;
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);
            evento = JsonSerializer.Deserialize<LancamentoRegistradoEvent>(json, Json);
            if (evento is null) throw new JsonException("Payload nulo.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Mensagem inválida (DeliveryTag {Tag}) — enviando para DLQ.", ea.DeliveryTag);
            _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        try
        {
            await _retry.ExecuteAsync(async _ =>
            {
                using var scope = scopeFactory.CreateScope();
                var sender2 = scope.ServiceProvider.GetRequiredService<ISender>();

                var command = new ConsolidarLancamentoCommand(
                    evento.EventId, evento.LancamentoId, evento.ComercianteId,
                    evento.Valor, evento.Tipo, evento.Data);

                var result = await sender2.Send(command);
                if (result.IsFailure)
                    throw new InvalidOperationException(result.Error.Message);
            });

            _channel!.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            // Erro persistente após retries: devolve à fila para nova tentativa futura.
            logger.LogWarning(ex, "Falha ao consolidar evento {EventId}; recolocando na fila.", evento.EventId);
            _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
