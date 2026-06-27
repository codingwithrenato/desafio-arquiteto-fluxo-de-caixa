using BuildingBlocks.Messaging;

namespace BuildingBlocks.Contracts;

/// <summary>
/// Evento de integração publicado pelo serviço de Lançamentos sempre que um
/// lançamento (crédito/débito) é registrado. Consumido pelo serviço de Consolidado
/// para projetar o saldo diário. É a fronteira de contrato entre os dois serviços.
/// </summary>
/// <param name="LancamentoId">Id natural do lançamento — chave de idempotência no consumidor.</param>
/// <param name="ComercianteId">Identificador do comerciante dono do fluxo de caixa.</param>
/// <param name="Valor">Valor absoluto (sempre positivo) do lançamento.</param>
/// <param name="Tipo">Crédito ou Débito.</param>
/// <param name="Data">Dia de competência do lançamento (usado na consolidação diária).</param>
public sealed record LancamentoRegistradoEvent(
    Guid LancamentoId,
    string ComercianteId,
    decimal Valor,
    TipoLancamento Tipo,
    DateOnly Data,
    Guid EventId,
    DateTime OccurredOnUtc) : IIntegrationEvent;
