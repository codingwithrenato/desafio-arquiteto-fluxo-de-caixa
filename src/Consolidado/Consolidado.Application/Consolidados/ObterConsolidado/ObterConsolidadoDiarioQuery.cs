using BuildingBlocks.Results;
using Consolidado.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Consolidado.Application.Consolidados.ObterConsolidado;

/// <summary>Query do saldo consolidado de um comerciante em um dia.</summary>
public sealed record ObterConsolidadoDiarioQuery(string ComercianteId, DateOnly Data)
    : IRequest<Result<SaldoConsolidadoDto>>;

/// <summary>
/// Estratégia read-through: tenta o cache (Redis); em miss, busca a projeção no banco,
/// popula o cache e retorna. É o que sustenta o pico de leitura sem recalcular saldo.
/// </summary>
public sealed class ObterConsolidadoDiarioQueryHandler(
    ISaldoConsolidadoCache cache,
    ISaldoDiarioRepository repository,
    ILogger<ObterConsolidadoDiarioQueryHandler> logger)
    : IRequestHandler<ObterConsolidadoDiarioQuery, Result<SaldoConsolidadoDto>>
{
    public async Task<Result<SaldoConsolidadoDto>> Handle(
        ObterConsolidadoDiarioQuery query, CancellationToken cancellationToken)
    {
        var emCache = await cache.GetAsync(query.ComercianteId, query.Data, cancellationToken);
        if (emCache is not null)
            return Result.Success(emCache);

        var saldo = await repository.GetAsync(query.ComercianteId, query.Data, cancellationToken);
        if (saldo is null)
            return Result.Failure<SaldoConsolidadoDto>(
                Error.NotFound($"Não há consolidado para {query.ComercianteId} em {query.Data:yyyy-MM-dd}."));

        var dto = new SaldoConsolidadoDto(
            saldo.ComercianteId, saldo.Data, saldo.TotalCreditos, saldo.TotalDebitos,
            saldo.Saldo, saldo.QuantidadeLancamentos, saldo.AtualizadoEmUtc, saldo.Fechado);

        await cache.SetAsync(dto, cancellationToken);
        logger.LogDebug("Cache miss para {ComercianteId}/{Data} — populando.", query.ComercianteId, query.Data);

        return Result.Success(dto);
    }
}
