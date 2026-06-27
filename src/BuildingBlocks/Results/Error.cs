namespace BuildingBlocks.Results;

/// <summary>
/// Representa uma falha de negócio de forma explícita, sem lançar exceções
/// para fluxo de controle. Usado pelo <see cref="Result"/> / <see cref="Result{T}"/>.
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string message) => new("validation", message);
    public static Error NotFound(string message) => new("not_found", message);
    public static Error Conflict(string message) => new("conflict", message);
}
