using System.Diagnostics;
using System.Text.Json;
using BuildingBlocks.Contracts;
using BuildingBlocks.Messaging;
using Lancamentos.Application.Abstractions;
using Lancamentos.Infrastructure.Persistence;

namespace Lancamentos.Infrastructure.Outbox;

/// <summary>
/// Implementação do padrão Outbox: serializa o evento de integração e o adiciona
/// ao <see cref="LancamentosDbContext"/>. A linha só é persistida quando o caso de
/// uso chama SaveChanges (mesma transação do lançamento) — daí a atomicidade.
/// </summary>
public sealed class OutboxWriter(LancamentosDbContext context) : IOutbox
{
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public void Enqueue<TEvent>(TEvent integrationEvent) where TEvent : IIntegrationEvent
    {
        var message = new OutboxMessage
        {
            Id = integrationEvent.EventId,
            Tipo = typeof(TEvent).Name,
            RoutingKey = ResolveRoutingKey(integrationEvent),
            Conteudo = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions),
            OccurredOnUtc = integrationEvent.OccurredOnUtc,
            // Captura o trace da requisição atual para correlacionar a consolidação futura.
            TraceParent = Activity.Current?.Id,
            Tentativas = 0
        };

        context.OutboxMessages.Add(message);
    }

    private static string ResolveRoutingKey(IIntegrationEvent integrationEvent) => integrationEvent switch
    {
        LancamentoRegistradoEvent => MessagingTopology.LancamentoRegistradoRoutingKey,
        _ => throw new NotSupportedException(
            $"Evento de integração sem routing key mapeada: {integrationEvent.GetType().Name}")
    };
}
