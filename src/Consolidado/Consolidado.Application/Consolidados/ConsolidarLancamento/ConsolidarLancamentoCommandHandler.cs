using System.Diagnostics;
using BuildingBlocks.Contracts;
using BuildingBlocks.Results;
using Consolidado.Application.Abstractions;
using Consolidado.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Consolidado.Application.Consolidados.ConsolidarLancamento;

/// <summary>
/// Aplica um lançamento à projeção de saldo diário. Garantias:
/// - Idempotência: se o EventId já foi processado, ignora (reentrega não duplica saldo).
/// - Atomicidade: atualização do saldo + marca de idempotência na MESMA transação.
/// - Cache: invalida a entrada do dia para que a próxima leitura reflita o novo saldo.
/// </summary>
public sealed class ConsolidarLancamentoCommandHandler(
    ISaldoDiarioRepository repository,
    IEventoProcessadoStore processados,
    ISaldoConsolidadoCache cache,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ConsolidarLancamentoCommandHandler> logger)
    : IRequestHandler<ConsolidarLancamentoCommand, Result>
{
    public async Task<Result> Handle(ConsolidarLancamentoCommand command, CancellationToken cancellationToken)
    {
        if (await processados.JaProcessadoAsync(command.EventId, cancellationToken))
        {
            Activity.Current?.AddEvent(new ActivityEvent("idempotencia: evento já processado"));
            logger.LogInformation("Evento {EventId} já processado — ignorando (idempotência).", command.EventId);
            return Result.Success();
        }

        var saldo = await repository.GetAsync(command.ComercianteId, command.Data, cancellationToken);
        if (saldo is null)
        {
            saldo = SaldoDiario.Criar(command.ComercianteId, command.Data, clock.UtcNow);
            await repository.AddAsync(saldo, cancellationToken);
        }

        switch (command.Tipo)
        {
            case TipoLancamento.Credito:
                saldo.AplicarCredito(command.Valor, clock.UtcNow);
                break;
            case TipoLancamento.Debito:
                saldo.AplicarDebito(command.Valor, clock.UtcNow);
                break;
            default:
                return Result.Failure(Error.Validation($"Tipo de lançamento inválido: {command.Tipo}."));
        }

        await processados.MarcarComoProcessadoAsync(command.EventId, clock.UtcNow, cancellationToken);

        // Persiste saldo + idempotência atomicamente.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Write-through: grava o valor autoritativo recém-calculado no cache. Preferimos
        // write-through à simples invalidação porque a invalidação tem uma race conhecida —
        // um leitor lento pode repovoar o cache com um valor antigo logo após a invalidação,
        // deixando-o velho até o TTL. Com write-through, cada evento reafirma o valor correto.
        var dto = new SaldoConsolidadoDto(
            saldo.ComercianteId, saldo.Data, saldo.TotalCreditos, saldo.TotalDebitos,
            saldo.Saldo, saldo.QuantidadeLancamentos, saldo.AtualizadoEmUtc, saldo.Fechado);
        await cache.SetAsync(dto, cancellationToken);

        Activity.Current?.AddEvent(new ActivityEvent("consolidado atualizado", tags: new ActivityTagsCollection
        {
            ["comerciante"] = command.ComercianteId,
            ["saldo"] = saldo.Saldo,
        }));

        logger.LogInformation(
            "Lançamento {LancamentoId} consolidado ({Tipo} {Valor}). Saldo de {ComercianteId} em {Data}: {Saldo}.",
            command.LancamentoId, command.Tipo, command.Valor, command.ComercianteId, command.Data, saldo.Saldo);

        return Result.Success();
    }
}
