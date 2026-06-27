using Consolidado.Application.Abstractions;
using Consolidado.Application.Consolidados;
using Consolidado.Application.Consolidados.ObterConsolidado;
using Consolidado.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Consolidado.UnitTests.Application;

public class ObterConsolidadoDiarioQueryHandlerTests
{
    private readonly ISaldoConsolidadoCache _cache = Substitute.For<ISaldoConsolidadoCache>();
    private readonly ISaldoDiarioRepository _repository = Substitute.For<ISaldoDiarioRepository>();
    private readonly ObterConsolidadoDiarioQueryHandler _handler;

    private static readonly DateOnly Dia = new(2026, 6, 27);

    public ObterConsolidadoDiarioQueryHandlerTests()
        => _handler = new ObterConsolidadoDiarioQueryHandler(
            _cache, _repository, NullLogger<ObterConsolidadoDiarioQueryHandler>.Instance);

    [Fact]
    public async Task Handle_CacheHit_NaoDeveTocarOBanco()
    {
        var dto = new SaldoConsolidadoDto("c1", Dia, 100m, 40m, 60m, 2, DateTime.UtcNow, false);
        _cache.GetAsync("c1", Dia, Arg.Any<CancellationToken>()).Returns(dto);

        var resultado = await _handler.Handle(new ObterConsolidadoDiarioQuery("c1", Dia), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Saldo.Should().Be(60m);
        await _repository.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMiss_DeveBuscarNoBancoEPopularCache()
    {
        _cache.GetAsync("c1", Dia, Arg.Any<CancellationToken>()).Returns((SaldoConsolidadoDto?)null);
        var saldo = SaldoDiario.Criar("c1", Dia, DateTime.UtcNow);
        saldo.AplicarCredito(300m, DateTime.UtcNow);
        _repository.GetAsync("c1", Dia, Arg.Any<CancellationToken>()).Returns(saldo);

        var resultado = await _handler.Handle(new ObterConsolidadoDiarioQuery("c1", Dia), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Saldo.Should().Be(300m);
        await _cache.Received(1).SetAsync(Arg.Is<SaldoConsolidadoDto>(d => d.Saldo == 300m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NaoEncontrado_DeveRetornarFalha()
    {
        _cache.GetAsync("c1", Dia, Arg.Any<CancellationToken>()).Returns((SaldoConsolidadoDto?)null);
        _repository.GetAsync("c1", Dia, Arg.Any<CancellationToken>()).Returns((SaldoDiario?)null);

        var resultado = await _handler.Handle(new ObterConsolidadoDiarioQuery("c1", Dia), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("not_found");
    }
}
