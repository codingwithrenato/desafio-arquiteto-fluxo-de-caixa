using BuildingBlocks.Contracts;
using Consolidado.Application.Abstractions;
using Consolidado.Application.Consolidados.ConsolidarLancamento;
using Consolidado.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Consolidado.UnitTests.Application;

public class ConsolidarLancamentoCommandHandlerTests
{
    private readonly ISaldoDiarioRepository _repository = Substitute.For<ISaldoDiarioRepository>();
    private readonly IEventoProcessadoStore _processados = Substitute.For<IEventoProcessadoStore>();
    private readonly ISaldoConsolidadoCache _cache = Substitute.For<ISaldoConsolidadoCache>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ConsolidarLancamentoCommandHandler _handler;

    private static readonly DateOnly Dia = new(2026, 6, 27);

    public ConsolidarLancamentoCommandHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 6, 27, 10, 0, 0, DateTimeKind.Utc));
        _handler = new ConsolidarLancamentoCommandHandler(
            _repository, _processados, _cache, _unitOfWork, _clock,
            NullLogger<ConsolidarLancamentoCommandHandler>.Instance);
    }

    private ConsolidarLancamentoCommand Comando(TipoLancamento tipo, decimal valor, Guid? eventId = null) =>
        new(eventId ?? Guid.NewGuid(), Guid.NewGuid(), "c1", valor, tipo, Dia);

    [Fact]
    public async Task Handle_EventoJaProcessado_DeveIgnorarSemAlterarSaldo()
    {
        var comando = Comando(TipoLancamento.Credito, 100m);
        _processados.JaProcessadoAsync(comando.EventId, Arg.Any<CancellationToken>()).Returns(true);

        var resultado = await _handler.Handle(comando, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _repository.DidNotReceive().AddAsync(Arg.Any<SaldoDiario>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NovoComerciante_DeveCriarSaldoAplicarMarcarESalvar()
    {
        var comando = Comando(TipoLancamento.Credito, 200m);
        _processados.JaProcessadoAsync(comando.EventId, Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetAsync("c1", Dia, Arg.Any<CancellationToken>()).Returns((SaldoDiario?)null);

        var resultado = await _handler.Handle(comando, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Is<SaldoDiario>(s => s.TotalCreditos == 200m), Arg.Any<CancellationToken>());
        await _processados.Received(1).MarcarComoProcessadoAsync(comando.EventId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync("c1", Dia, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SaldoExistente_DeveAplicarDebito()
    {
        var saldoExistente = SaldoDiario.Criar("c1", Dia, _clock.UtcNow);
        saldoExistente.AplicarCredito(500m, _clock.UtcNow);

        var comando = Comando(TipoLancamento.Debito, 120m);
        _processados.JaProcessadoAsync(comando.EventId, Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetAsync("c1", Dia, Arg.Any<CancellationToken>()).Returns(saldoExistente);

        await _handler.Handle(comando, CancellationToken.None);

        saldoExistente.TotalDebitos.Should().Be(120m);
        saldoExistente.Saldo.Should().Be(380m);
        await _repository.DidNotReceive().AddAsync(Arg.Any<SaldoDiario>(), Arg.Any<CancellationToken>());
    }
}
