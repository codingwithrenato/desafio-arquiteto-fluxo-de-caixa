namespace Lancamentos.Infrastructure.Messaging;

/// <summary>
/// Publicação de baixo nível no broker. Usada pelo OutboxDispatcher para publicar
/// o conteúdo JÁ serializado da mensagem de outbox, sem reconstruir o tipo do evento.
/// </summary>
public interface IMessageBusPublisher
{
    /// <summary>
    /// Publica o corpo no exchange com a routing key indicada, de forma persistente
    /// e com publisher confirms (lança se o broker não confirmar). Os <paramref name="headers"/>
    /// opcionais carregam o contexto de trace (W3C) para propagação ao consumidor.
    /// </summary>
    void Publish(string routingKey, ReadOnlyMemory<byte> body, string messageId,
        IDictionary<string, object>? headers = null);
}
