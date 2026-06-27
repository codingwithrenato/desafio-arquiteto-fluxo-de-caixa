using BuildingBlocks.Contracts;
using BuildingBlocks.Results;
using Lancamentos.Application.Abstractions;
using Lancamentos.Domain;
using MediatR;
using Microsoft.Extensions.Logging;
using ContratoTipo = BuildingBlocks.Contracts.TipoLancamento;
using DominioTipo = Lancamentos.Domain.TipoLancamento;

namespace Lancamentos.Application.Lancamentos.RegistrarLancamento;

/// <summary>
/// Orquestra o registro do lançamento:
/// 1) cria o aggregate (invariantes de domínio);
/// 2) adiciona ao repositório;
/// 3) enfileira o evento de integração no Outbox;
/// 4) confirma TUDO em uma única transação (Unit of Work).
/// O publish no broker acontece depois, de forma assíncrona, pelo dispatcher.
/// </summary>
public sealed class RegistrarLancamentoCommandHandler(
    ILancamentoRepository repository,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RegistrarLancamentoCommandHandler> logger)
    : IRequestHandler<RegistrarLancamentoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegistrarLancamentoCommand command, CancellationToken cancellationToken)
    {
        var data = command.Data ?? clock.Today;
        var agoraUtc = clock.UtcNow;

        Lancamento lancamento;
        try
        {
            lancamento = Lancamento.Registrar(
                command.ComercianteId,
                command.Valor,
                command.Tipo,
                data,
                agoraUtc,
                command.Descricao);
        }
        catch (DomainException ex)
        {
            return Result.Failure<Guid>(Error.Validation(ex.Message));
        }

        await repository.AddAsync(lancamento, cancellationToken);

        var evento = new LancamentoRegistradoEvent(
            LancamentoId: lancamento.Id,
            ComercianteId: lancamento.ComercianteId,
            Valor: lancamento.Valor.Amount,
            Tipo: MapTipo(lancamento.Tipo),
            Data: lancamento.Data,
            EventId: Guid.NewGuid(),
            OccurredOnUtc: agoraUtc);

        outbox.Enqueue(evento);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Lançamento {LancamentoId} registrado ({Tipo} {Valor}) e evento enfileirado no outbox.",
            lancamento.Id, lancamento.Tipo, lancamento.Valor);

        return Result.Success(lancamento.Id);
    }

    private static ContratoTipo MapTipo(DominioTipo tipo) => tipo switch
    {
        DominioTipo.Credito => ContratoTipo.Credito,
        DominioTipo.Debito => ContratoTipo.Debito,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de lançamento desconhecido.")
    };
}
