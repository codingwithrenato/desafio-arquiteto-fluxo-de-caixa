namespace Lancamentos.Application.Abstractions;

/// <summary>Abstração de relógio para tornar os casos de uso determinísticos em testes.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
