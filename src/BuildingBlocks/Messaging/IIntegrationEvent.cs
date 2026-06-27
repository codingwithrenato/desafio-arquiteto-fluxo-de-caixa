namespace BuildingBlocks.Messaging;

/// <summary>
/// Contrato base de um evento de integração trafegado entre serviços via broker.
/// Eventos de integração formam o "shared kernel" entre Lançamentos e Consolidado:
/// um contrato estável e versionável, independente dos modelos de domínio internos.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Identificador único da mensagem — usado para idempotência/deduplicação.</summary>
    Guid EventId { get; }

    /// <summary>Momento em que o evento ocorreu (UTC).</summary>
    DateTime OccurredOnUtc { get; }
}
