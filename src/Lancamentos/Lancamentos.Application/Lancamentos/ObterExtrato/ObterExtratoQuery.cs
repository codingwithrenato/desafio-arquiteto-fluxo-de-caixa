using BuildingBlocks.Results;
using Lancamentos.Application.Abstractions;
using Lancamentos.Application.Lancamentos.ObterLancamento;
using Lancamentos.Domain;
using MediatR;

namespace Lancamentos.Application.Lancamentos.ObterExtrato;

/// <summary>Query (CQRS) do extrato de lançamentos de um comerciante em um dia.</summary>
public sealed record ObterExtratoQuery(string ComercianteId, DateOnly Data) : IRequest<Result<ExtratoDto>>;

public sealed class ObterExtratoQueryHandler(ILancamentoRepository repository)
    : IRequestHandler<ObterExtratoQuery, Result<ExtratoDto>>
{
    public async Task<Result<ExtratoDto>> Handle(ObterExtratoQuery query, CancellationToken cancellationToken)
    {
        var lancamentos = await repository.GetPorComercianteEDataAsync(
            query.ComercianteId, query.Data, cancellationToken);

        var itens = lancamentos
            .Select(l => new LancamentoDto(
                l.Id, l.ComercianteId, l.Valor.Amount, l.Tipo, l.Data, l.CriadoEmUtc, l.Descricao))
            .ToList();

        var totalCreditos = lancamentos.Where(l => l.Tipo == TipoLancamento.Credito).Sum(l => l.Valor.Amount);
        var totalDebitos = lancamentos.Where(l => l.Tipo == TipoLancamento.Debito).Sum(l => l.Valor.Amount);

        // Dia sem lançamentos retorna extrato vazio (lista vazia + zeros), não uma falha.
        var extrato = new ExtratoDto(
            query.ComercianteId, query.Data, itens,
            totalCreditos, totalDebitos, totalCreditos - totalDebitos, itens.Count);

        return Result.Success(extrato);
    }
}
