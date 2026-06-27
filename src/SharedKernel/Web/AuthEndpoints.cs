using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel.Security;

namespace SharedKernel.Web;

public static class AuthEndpoints
{
    public sealed record TokenRequest(string ComercianteId);

    /// <summary>
    /// Endpoint de autenticação SIMPLIFICADO para demonstração: emite um JWT para o
    /// comerciante informado. Em produção, a autenticação fica num Identity Provider
    /// dedicado — aqui o objetivo é tornar o fluxo executável de ponta a ponta.
    /// </summary>
    public static IEndpointRouteBuilder MapDevAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/token", (TokenRequest request, JwtTokenService tokens) =>
        {
            if (string.IsNullOrWhiteSpace(request.ComercianteId))
                return Results.BadRequest(new { mensagem = "ComercianteId é obrigatório." });

            var (token, expiraEm) = tokens.Emitir(request.ComercianteId, roles: ["comerciante"]);
            return Results.Ok(new { access_token = token, token_type = "Bearer", expires_at = expiraEm });
        })
        .WithTags("Autenticação")
        .WithSummary("Emite um token JWT de demonstração.")
        .AllowAnonymous();

        return app;
    }
}
