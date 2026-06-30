using BuildingBlocks.Results;
using Lancamentos.Application.Lancamentos.ObterExtrato;
using Lancamentos.Application.Lancamentos.ObterLancamento;
using Lancamentos.Application.Lancamentos.RegistrarLancamento;
using Lancamentos.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lancamentos.API.Endpoints;

/// <summary>Endpoints HTTP do serviço de Lançamentos (Minimal API).</summary>
public static class LancamentosEndpoints
{
    public sealed record RegistrarLancamentoRequest(
        string ComercianteId,
        decimal Valor,
        TipoLancamento Tipo,
        DateOnly? Data,
        string? Descricao);

    public static IEndpointRouteBuilder MapLancamentosEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/lancamentos")
            .WithTags("Lançamentos")
            .RequireAuthorization();

        grupo.MapPost("/", RegistrarAsync)
            .WithName("RegistrarLancamento")
            .WithSummary("Registra um lançamento de débito ou crédito.")
            .Produces(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status400BadRequest);

        grupo.MapGet("/{id:guid}", ObterAsync)
            .WithName("ObterLancamento")
            .WithSummary("Consulta um lançamento pelo seu identificador.")
            .Produces<LancamentoDto>()
            .Produces(StatusCodes.Status404NotFound);

        grupo.MapGet("/extrato/{comercianteId}/{data}", ObterExtratoAsync)
            .WithName("ObterExtrato")
            .WithSummary("Lista os lançamentos de um comerciante em um dia (extrato) com os totais.")
            .Produces<ExtratoDto>();

        return app;
    }

    private static async Task<IResult> RegistrarAsync(
        RegistrarLancamentoRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RegistrarLancamentoCommand(
            request.ComercianteId, request.Valor, request.Tipo, request.Data, request.Descricao);

        Result<Guid> resultado = await sender.Send(command, cancellationToken);

        if (resultado.IsFailure)
            return Results.BadRequest(new { erro = resultado.Error.Code, mensagem = resultado.Error.Message });

        // 202 Accepted: o lançamento foi aceito e persistido; a consolidação é assíncrona.
        return Results.AcceptedAtRoute("ObterLancamento", new { id = resultado.Value }, new { id = resultado.Value });
    }

    private static async Task<IResult> ObterAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var resultado = await sender.Send(new ObterLancamentoQuery(id), cancellationToken);

        return resultado.IsSuccess
            ? Results.Ok(resultado.Value)
            : Results.NotFound(new { mensagem = resultado.Error.Message });
    }

    private static async Task<IResult> ObterExtratoAsync(
        string comercianteId,
        DateOnly data,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var resultado = await sender.Send(new ObterExtratoQuery(comercianteId, data), cancellationToken);
        return Results.Ok(resultado.Value);
    }
}
