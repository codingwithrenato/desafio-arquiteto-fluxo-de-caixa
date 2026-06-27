using BuildingBlocks.Contracts;
using FluentAssertions;
using Lancamentos.Application.Abstractions;
using Lancamentos.Application.Lancamentos.RegistrarLancamento;
using Lancamentos.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ContratoTipo = BuildingBlocks.Contracts.TipoLancamento;
using DominioTipo = Lancamentos.Domain.TipoLancamento;

namespace Lancamentos.UnitTests.Application;

public class RegistrarLancamentoCommandHandlerTests
{
    private readonly ILancamentoRepository _repository = Substitute.For<ILancamentoRepository>();
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly RegistrarLancamentoCommandHandler _handler;

    public RegistrarLancamentoCommandHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTime(2026, 6, 27, 10, 0, 0, DateTimeKind.Utc));
        _clock.Today.Returns(new DateOnly(2026, 6, 27));
        _handler = new RegistrarLancamentoCommandHandler(
            _repository, _outbox, _unitOfWork, _clock, NullLogger<RegistrarLancamentoCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_DevePersistirLancamentoEnfileirarEventoESalvar()
    {
        var command = new RegistrarLancamentoCommand("c1", 200m, DominioTipo.Credito);

        var resultado = await _handler.Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeEmpty();

        await _repository.Received(1).AddAsync(Arg.Is<Lancamento>(l => l.ComercianteId == "c1"), Arg.Any<CancellationToken>());
        _outbox.Received(1).Enqueue(Arg.Is<LancamentoRegistradoEvent>(e =>
            e.ComercianteId == "c1" && e.Valor == 200m && e.Tipo == ContratoTipo.Credito));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeveUsarDataDeHojeQuandoNaoInformada()
    {
        var command = new RegistrarLancamentoCommand("c1", 50m, DominioTipo.Debito);

        await _handler.Handle(command, CancellationToken.None);

        _outbox.Received(1).Enqueue(Arg.Is<LancamentoRegistradoEvent>(e => e.Data == new DateOnly(2026, 6, 27)));
    }

    [Fact]
    public async Task Handle_DeveRetornarFalhaParaValorInvalido()
    {
        var command = new RegistrarLancamentoCommand("c1", -10m, DominioTipo.Credito);

        var resultado = await _handler.Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("validation");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
