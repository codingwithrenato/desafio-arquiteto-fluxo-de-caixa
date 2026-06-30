using FluentAssertions;
using Lancamentos.Application.Abstractions;
using Lancamentos.Application.Lancamentos.ObterExtrato;
using Lancamentos.Domain;
using NSubstitute;

namespace Lancamentos.UnitTests.Application;

public class ObterExtratoQueryHandlerTests
{
    private readonly ILancamentoRepository _repository = Substitute.For<ILancamentoRepository>();
    private readonly ObterExtratoQueryHandler _handler;

    private static readonly DateOnly Dia = new(2026, 6, 29);
    private static readonly DateTime AgoraUtc = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    public ObterExtratoQueryHandlerTests() => _handler = new ObterExtratoQueryHandler(_repository);

    [Fact]
    public async Task Handle_DeveListarItensECalcularTotais()
    {
        var lancamentos = new List<Lancamento>
        {
            Lancamento.Registrar("c1", 100m, TipoLancamento.Credito, Dia, AgoraUtc),
            Lancamento.Registrar("c1", 50m, TipoLancamento.Credito, Dia, AgoraUtc),
            Lancamento.Registrar("c1", 30m, TipoLancamento.Debito, Dia, AgoraUtc),
        };
        _repository.GetPorComercianteEDataAsync("c1", Dia, Arg.Any<CancellationToken>()).Returns(lancamentos);

        var resultado = await _handler.Handle(new ObterExtratoQuery("c1", Dia), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var extrato = resultado.Value;
        extrato.Itens.Should().HaveCount(3);
        extrato.TotalCreditos.Should().Be(150m);
        extrato.TotalDebitos.Should().Be(30m);
        extrato.Saldo.Should().Be(120m);
        extrato.Quantidade.Should().Be(3);
    }

    [Fact]
    public async Task Handle_DiaSemLancamentos_DeveRetornarExtratoVazio()
    {
        _repository.GetPorComercianteEDataAsync("c1", Dia, Arg.Any<CancellationToken>())
            .Returns(new List<Lancamento>());

        var resultado = await _handler.Handle(new ObterExtratoQuery("c1", Dia), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Itens.Should().BeEmpty();
        resultado.Value.Saldo.Should().Be(0m);
        resultado.Value.Quantidade.Should().Be(0);
    }
}
