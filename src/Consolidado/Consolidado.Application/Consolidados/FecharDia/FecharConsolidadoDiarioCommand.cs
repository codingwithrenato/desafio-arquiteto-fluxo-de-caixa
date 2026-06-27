using BuildingBlocks.Results;
using Consolidado.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Consolidado.Application.Consolidados.FecharDia;

/// <summary>
/// Fecha o consolidado de um dia para todos os comerciantes. Acionado pelo job
/// recorrente do Hangfire (fim do dia). Após fechado, o saldo do dia vira um
/// snapshot imutável — novos eventos daquele dia são rejeitados na consolidação.
/// </summary>
public sealed record FecharConsolidadoDiarioCommand(DateOnly Data) : IRequest<Result<int>>;

public sealed class FecharConsolidadoDiarioCommandHandler(
    ISaldoDiarioRepository repository,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<FecharConsolidadoDiarioCommandHandler> logger)
    : IRequestHandler<FecharConsolidadoDiarioCommand, Result<int>>
{
    public async Task<Result<int>> Handle(FecharConsolidadoDiarioCommand command, CancellationToken cancellationToken)
    {
        var saldos = await repository.GetPorDataAsync(command.Data, cancellationToken);
        var fechados = 0;

        foreach (var saldo in saldos.Where(s => !s.Fechado))
        {
            saldo.Fechar(clock.UtcNow);
            fechados++;
        }

        if (fechados > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Fechamento diário de {Data}: {Fechados} consolidados fechados.", command.Data, fechados);
        return Result.Success(fechados);
    }
}
