using Consolidado.Domain;
using FluentAssertions;

namespace Consolidado.UnitTests.Domain;

public class SaldoDiarioTests
{
    private static readonly DateOnly Dia = new(2026, 6, 27);
    private static readonly DateTime AgoraUtc = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Criar_DeveIniciarZerado()
    {
        var saldo = SaldoDiario.Criar("c1", Dia, AgoraUtc);

        saldo.TotalCreditos.Should().Be(0);
        saldo.TotalDebitos.Should().Be(0);
        saldo.Saldo.Should().Be(0);
        saldo.QuantidadeLancamentos.Should().Be(0);
        saldo.Fechado.Should().BeFalse();
    }

    [Fact]
    public void AplicarCreditosEDebitos_DeveCalcularSaldoEContagem()
    {
        var saldo = SaldoDiario.Criar("c1", Dia, AgoraUtc);

        saldo.AplicarCredito(100m, AgoraUtc);
        saldo.AplicarCredito(50m, AgoraUtc);
        saldo.AplicarDebito(30m, AgoraUtc);

        saldo.TotalCreditos.Should().Be(150m);
        saldo.TotalDebitos.Should().Be(30m);
        saldo.Saldo.Should().Be(120m);
        saldo.QuantidadeLancamentos.Should().Be(3);
    }

    [Fact]
    public void Fechar_DeveImpedirNovosLancamentos()
    {
        var saldo = SaldoDiario.Criar("c1", Dia, AgoraUtc);
        saldo.AplicarCredito(100m, AgoraUtc);

        saldo.Fechar(AgoraUtc);

        saldo.Fechado.Should().BeTrue();
        var acao = () => saldo.AplicarCredito(10m, AgoraUtc);
        acao.Should().Throw<InvalidOperationException>();
    }
}
