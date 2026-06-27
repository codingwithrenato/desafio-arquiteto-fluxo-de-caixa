using System.Text;
using Lancamentos.Infrastructure.Messaging;
using Lancamentos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lancamentos.Infrastructure.Outbox;

/// <summary>
/// Processo de background que drena a tabela de Outbox e publica os eventos no
/// RabbitMQ. Roda dentro da própria API de Lançamentos (BackgroundService).
///
/// Pontos de robustez:
/// - <c>FOR UPDATE SKIP LOCKED</c>: várias réplicas podem rodar o dispatcher sem
///   processar a mesma mensagem duas vezes nem bloquear umas às outras.
/// - Falha de publicação não perde a mensagem: incrementa tentativas e tenta de novo.
/// - Se o broker estiver fora, as mensagens simplesmente acumulam e são publicadas
///   quando ele voltar (catch-up) — a API de Lançamentos continua disponível.
/// </summary>
public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IMessageBusPublisher publisher,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = TimeSpan.FromSeconds(_options.PollingIntervalSeconds);
        logger.LogInformation("OutboxDispatcher iniciado (intervalo {Intervalo}s, lote {Lote}).",
            _options.PollingIntervalSeconds, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessarLoteAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no ciclo do OutboxDispatcher.");
            }

            await Task.Delay(intervalo, stoppingToken);
        }
    }

    private async Task ProcessarLoteAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var pendentes = await db.OutboxMessages
            .FromSqlInterpolated($@"
                SELECT * FROM outbox_messages
                WHERE processado_em_utc IS NULL AND tentativas < {_options.MaxTentativas}
                ORDER BY occurred_on_utc
                LIMIT {_options.BatchSize}
                FOR UPDATE SKIP LOCKED")
            .ToListAsync(cancellationToken);

        if (pendentes.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        foreach (var mensagem in pendentes)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(mensagem.Conteudo);
                publisher.Publish(mensagem.RoutingKey, body, mensagem.Id.ToString());

                mensagem.ProcessadoEmUtc = DateTime.UtcNow;
                mensagem.UltimoErro = null;
            }
            catch (Exception ex)
            {
                mensagem.Tentativas++;
                mensagem.UltimoErro = ex.Message;
                logger.LogWarning(ex,
                    "Falha ao publicar mensagem de outbox {MessageId} (tentativa {Tentativa}).",
                    mensagem.Id, mensagem.Tentativas);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var publicadas = pendentes.Count(m => m.ProcessadoEmUtc is not null);
        if (publicadas > 0)
            logger.LogInformation("OutboxDispatcher publicou {Publicadas}/{Total} mensagens.",
                publicadas, pendentes.Count);
    }
}
