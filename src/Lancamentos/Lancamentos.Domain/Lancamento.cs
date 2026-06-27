namespace Lancamentos.Domain;

/// <summary>
/// Aggregate root do serviço de Lançamentos. Representa um débito ou crédito
/// no fluxo de caixa de um comerciante em um determinado dia.
/// Imutável após a criação — um lançamento é um fato contábil.
/// </summary>
public sealed class Lancamento
{
    public Guid Id { get; private set; }
    public string ComercianteId { get; private set; } = default!;
    public Money Valor { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public DateOnly Data { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }
    public string? Descricao { get; private set; }

    // Construtor sem parâmetros para o EF Core.
    private Lancamento() { }

    private Lancamento(Guid id, string comercianteId, Money valor, TipoLancamento tipo, DateOnly data, DateTime criadoEmUtc, string? descricao)
    {
        Id = id;
        ComercianteId = comercianteId;
        Valor = valor;
        Tipo = tipo;
        Data = data;
        CriadoEmUtc = criadoEmUtc;
        Descricao = descricao;
    }

    /// <summary>
    /// Factory method que cria um lançamento válido. Centraliza as invariantes
    /// de criação (valor positivo via <see cref="Money"/>, comerciante obrigatório).
    /// </summary>
    public static Lancamento Registrar(
        string comercianteId,
        decimal valor,
        TipoLancamento tipo,
        DateOnly data,
        DateTime criadoEmUtc,
        string? descricao = null,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(comercianteId))
            throw new DomainException("O comerciante é obrigatório.");

        return new Lancamento(
            id ?? Guid.NewGuid(),
            comercianteId.Trim(),
            Money.From(valor),
            tipo,
            data,
            criadoEmUtc,
            string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim());
    }

    /// <summary>Valor com sinal contábil: crédito positivo, débito negativo.</summary>
    public decimal ValorComSinal => Tipo == TipoLancamento.Credito ? Valor.Amount : -Valor.Amount;
}
