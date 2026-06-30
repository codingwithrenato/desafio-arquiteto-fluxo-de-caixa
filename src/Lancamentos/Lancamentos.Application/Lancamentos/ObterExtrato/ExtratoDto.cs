using Lancamentos.Application.Lancamentos.ObterLancamento;

namespace Lancamentos.Application.Lancamentos.ObterExtrato;

/// <summary>
/// Extrato de um comerciante em um dia: a lista de lançamentos individuais mais o
/// resumo (totais) calculado a partir dela. Auto-contido para a tela de extrato.
/// </summary>
public sealed record ExtratoDto(
    string ComercianteId,
    DateOnly Data,
    IReadOnlyList<LancamentoDto> Itens,
    decimal TotalCreditos,
    decimal TotalDebitos,
    decimal Saldo,
    int Quantidade);
