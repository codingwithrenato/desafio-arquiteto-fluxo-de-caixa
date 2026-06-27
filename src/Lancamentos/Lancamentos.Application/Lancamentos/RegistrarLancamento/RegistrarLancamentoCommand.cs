using BuildingBlocks.Results;
using Lancamentos.Domain;
using MediatR;

namespace Lancamentos.Application.Lancamentos.RegistrarLancamento;

/// <summary>
/// Caso de uso (Command/CQRS) de registro de um lançamento de débito ou crédito.
/// Retorna o Id do lançamento criado em caso de sucesso.
/// </summary>
public sealed record RegistrarLancamentoCommand(
    string ComercianteId,
    decimal Valor,
    TipoLancamento Tipo,
    DateOnly? Data = null,
    string? Descricao = null) : IRequest<Result<Guid>>;
