namespace Lancamentos.Domain;

/// <summary>
/// Value Object que representa um valor monetário. Encapsula a invariante
/// "valor deve ser positivo" — impossível construir um Money inválido.
/// Imutável e comparado por valor (record struct).
/// </summary>
public readonly record struct Money
{
    public decimal Amount { get; }

    private Money(decimal amount) => Amount = amount;

    public static Money From(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("O valor do lançamento deve ser maior que zero.");

        // Fluxo de caixa em BRL: 2 casas decimais.
        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven));
    }

    public override string ToString() => Amount.ToString("0.00");
}
