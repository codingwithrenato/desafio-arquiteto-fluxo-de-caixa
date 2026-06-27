using FluentAssertions;
using Lancamentos.Domain;

namespace Lancamentos.UnitTests.Domain;

public class MoneyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void From_DeveRejeitarValorNaoPositivo(decimal valor)
    {
        var acao = () => Money.From(valor);
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void From_DeveArredondarParaDuasCasas()
    {
        Money.From(10.005m).Amount.Should().Be(10.00m);
        Money.From(10.015m).Amount.Should().Be(10.02m);
    }

    [Fact]
    public void From_DeveManterValorValido()
    {
        Money.From(150.75m).Amount.Should().Be(150.75m);
    }
}
