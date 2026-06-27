using BuildingBlocks.Results;
using Lancamentos.Application.Abstractions;
using MediatR;

namespace Lancamentos.Application.Lancamentos.ObterLancamento;

/// <summary>Query (CQRS) que recupera um lançamento pelo seu Id.</summary>
public sealed record ObterLancamentoQuery(Guid Id) : IRequest<Result<LancamentoDto>>;

public sealed class ObterLancamentoQueryHandler(ILancamentoRepository repository)
    : IRequestHandler<ObterLancamentoQuery, Result<LancamentoDto>>
{
    public async Task<Result<LancamentoDto>> Handle(ObterLancamentoQuery query, CancellationToken cancellationToken)
    {
        var lancamento = await repository.GetByIdAsync(query.Id, cancellationToken);
        if (lancamento is null)
            return Result.Failure<LancamentoDto>(Error.NotFound($"Lançamento {query.Id} não encontrado."));

        var dto = new LancamentoDto(
            lancamento.Id,
            lancamento.ComercianteId,
            lancamento.Valor.Amount,
            lancamento.Tipo,
            lancamento.Data,
            lancamento.CriadoEmUtc,
            lancamento.Descricao);

        return Result.Success(dto);
    }
}
