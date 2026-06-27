using FluentValidation;

namespace Lancamentos.Application.Lancamentos.RegistrarLancamento;

/// <summary>Validação de entrada (input) do comando. Erros aqui viram HTTP 400.</summary>
public sealed class RegistrarLancamentoCommandValidator : AbstractValidator<RegistrarLancamentoCommand>
{
    public RegistrarLancamentoCommandValidator()
    {
        RuleFor(x => x.ComercianteId)
            .NotEmpty().WithMessage("O comerciante é obrigatório.")
            .MaximumLength(100);

        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("Tipo de lançamento inválido.");

        RuleFor(x => x.Descricao)
            .MaximumLength(250);
    }
}
