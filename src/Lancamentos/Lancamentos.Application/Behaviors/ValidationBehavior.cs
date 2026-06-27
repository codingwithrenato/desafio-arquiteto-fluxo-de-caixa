using FluentValidation;
using MediatR;

namespace Lancamentos.Application.Behaviors;

/// <summary>
/// Pipeline behavior do MediatR que executa as validações FluentValidation
/// ANTES do handler. Falha de input lança <see cref="ValidationException"/>,
/// traduzida para HTTP 400 (ProblemDetails) pelo middleware da API.
/// Cross-cutting concern centralizado (DRY + Open/Closed).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
