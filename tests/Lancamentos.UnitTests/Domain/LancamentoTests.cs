using FluentAssertions;
using Lancamentos.Domain;

namespace Lancamentos.UnitTests.Domain;

public class LancamentoTests
{
    private static readonly DateOnly Hoje = new(2026, 6, 27);
    private static readonly DateTime AgoraUtc = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Registrar_DeveCriarLancamentoValido()
    {
        var lancamento = Lancamento.Registrar("comerciante-1", 100m, TipoLancamento.Credito, Hoje, AgoraUtc, "venda");

        lancamento.ComercianteId.Should().Be("comerciante-1");
        lancamento.Valor.Amount.Should().Be(100m);
        lancamento.Tipo.Should().Be(TipoLancamento.Credito);
        lancamento.Data.Should().Be(Hoje);
        lancamento.Descricao.Should().Be("venda");
        lancamento.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Registrar_DeveRejeitarComercianteVazio(string comerciante)
    {
        var acao = () => Lancamento.Registrar(comerciante, 100m, TipoLancamento.Credito, Hoje, AgoraUtc);
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Registrar_DeveRejeitarValorNaoPositivo()
    {
        var acao = () => Lancamento.Registrar("c1", 0m, TipoLancamento.Debito, Hoje, AgoraUtc);
        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void ValorComSinal_CreditoPositivoDebitoNegativo()
    {
        var credito = Lancamento.Registrar("c1", 100m, TipoLancamento.Credito, Hoje, AgoraUtc);
        var debito = Lancamento.Registrar("c1", 40m, TipoLancamento.Debito, Hoje, AgoraUtc);

        credito.ValorComSinal.Should().Be(100m);
        debito.ValorComSinal.Should().Be(-40m);
    }
}
