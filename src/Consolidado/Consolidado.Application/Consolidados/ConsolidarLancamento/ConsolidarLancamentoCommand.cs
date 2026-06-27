using BuildingBlocks.Contracts;
using BuildingBlocks.Results;
using MediatR;

namespace Consolidado.Application.Consolidados.ConsolidarLancamento;

/// <summary>
/// Caso de uso disparado pelo consumidor a cada evento de lançamento recebido.
/// Atualiza a projeção de saldo diário de forma idempotente.
/// </summary>
public sealed record ConsolidarLancamentoCommand(
    Guid EventId,
    Guid LancamentoId,
    string ComercianteId,
    decimal Valor,
    TipoLancamento Tipo,
    DateOnly Data) : IRequest<Result>;
