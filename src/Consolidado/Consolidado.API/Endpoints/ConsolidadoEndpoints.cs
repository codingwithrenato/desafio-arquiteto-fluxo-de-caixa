using Consolidado.Application.Consolidados;
using Consolidado.Application.Consolidados.ObterConsolidado;
using MediatR;

namespace Consolidado.API.Endpoints;

public static class ConsolidadoEndpoints
{
    public static IEndpointRouteBuilder MapConsolidadoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/consolidado")
            .WithTags("Consolidado Diário")
            .RequireAuthorization()
            .RequireRateLimiting("consolidado-read");

        grupo.MapGet("/{comercianteId}/{data}", ObterAsync)
            .WithName("ObterConsolidadoDiario")
            .WithSummary("Retorna o saldo consolidado de um comerciante em um dia (yyyy-MM-dd).")
            .Produces<SaldoConsolidadoDto>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ObterAsync(
        string comercianteId,
        DateOnly data,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var resultado = await sender.Send(new ObterConsolidadoDiarioQuery(comercianteId, data), cancellationToken);

        return resultado.IsSuccess
            ? Results.Ok(resultado.Value)
            : Results.NotFound(new { mensagem = resultado.Error.Message });
    }
}
