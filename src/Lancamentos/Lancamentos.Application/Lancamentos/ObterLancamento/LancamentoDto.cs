using Lancamentos.Domain;

namespace Lancamentos.Application.Lancamentos.ObterLancamento;

public sealed record LancamentoDto(
    Guid Id,
    string ComercianteId,
    decimal Valor,
    TipoLancamento Tipo,
    DateOnly Data,
    DateTime CriadoEmUtc,
    string? Descricao);
