using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Web;

/// <summary>
/// Tratamento global de exceções (IExceptionHandler do .NET 8). Converte falhas em
/// respostas ProblemDetails (RFC 7807) consistentes. Erros de validação de input
/// viram 400; o restante vira 500 sem vazar detalhes internos ao cliente.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case ValidationException validation:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Falha de validação",
                    Detail = "Um ou mais campos são inválidos.",
                    Type = "https://httpstatuses.com/400"
                };
                problem.Extensions["errors"] = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                break;

            default:
                logger.LogError(exception, "Exceção não tratada.");
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Erro interno",
                    Detail = "Ocorreu um erro inesperado.",
                    Type = "https://httpstatuses.com/500"
                };
                break;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
