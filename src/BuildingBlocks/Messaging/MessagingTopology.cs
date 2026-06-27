namespace BuildingBlocks.Messaging;

/// <summary>
/// Nomes de topologia do RabbitMQ compartilhados entre os serviços (shared kernel).
/// Centralizar aqui evita divergência de exchange/routing key entre publisher e consumer.
/// </summary>
public static class MessagingTopology
{
    /// <summary>Exchange durável do tipo topic por onde trafegam todos os eventos de integração.</summary>
    public const string Exchange = "fluxo-caixa.events";

    /// <summary>Routing key do evento de lançamento registrado.</summary>
    public const string LancamentoRegistradoRoutingKey = "lancamento.registrado";

    /// <summary>Fila durável do serviço de Consolidado para os eventos de lançamento.</summary>
    public const string ConsolidadoQueue = "consolidado.lancamentos-registrados";

    /// <summary>Dead-letter exchange/queue para mensagens que falham repetidamente.</summary>
    public const string DeadLetterExchange = "fluxo-caixa.events.dlx";
    public const string ConsolidadoDeadLetterQueue = "consolidado.lancamentos-registrados.dlq";
}
