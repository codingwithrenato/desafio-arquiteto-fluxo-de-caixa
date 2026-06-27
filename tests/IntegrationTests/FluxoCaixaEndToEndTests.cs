using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace IntegrationTests;

/// <summary>
/// Teste de integração do fluxo completo e DESACOPLADO:
/// POST /lancamentos (serviço A) → Outbox → RabbitMQ → consumidor (serviço B)
/// → projeção de saldo → GET /consolidado.
/// Valida o requisito central: os serviços se comunicam de forma assíncrona.
/// </summary>
public sealed class FluxoCaixaEndToEndTests(FluxoDeCaixaFixture fixture) : IClassFixture<FluxoDeCaixaFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record TokenResponse(string Access_Token);
    private sealed record SaldoResponse(
        string ComercianteId, decimal TotalCreditos, decimal TotalDebitos,
        decimal Saldo, int QuantidadeLancamentos);

    [Fact]
    public async Task LancamentosDevemSerConsolidadosDeFormaAssincrona()
    {
        var comerciante = $"comerciante-{Guid.NewGuid():N}";
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var lancamentos = await AutenticarAsync(fixture.Lancamentos.CreateClient(), comerciante);
        var consolidadoClient = await AutenticarAsync(fixture.Consolidado.CreateClient(), comerciante);

        // Registra crédito de 100, crédito de 50 e débito de 30 → saldo esperado 120.
        await PostLancamentoAsync(lancamentos, comerciante, 100m, tipo: 1, hoje);
        await PostLancamentoAsync(lancamentos, comerciante, 50m, tipo: 1, hoje);
        await PostLancamentoAsync(lancamentos, comerciante, 30m, tipo: 2, hoje);

        var saldo = await AguardarConsolidacaoAsync(consolidadoClient, comerciante, hoje,
            esperado: s => s.QuantidadeLancamentos == 3);

        saldo.Should().NotBeNull();
        saldo!.TotalCreditos.Should().Be(150m);
        saldo.TotalDebitos.Should().Be(30m);
        saldo.Saldo.Should().Be(120m);
        saldo.QuantidadeLancamentos.Should().Be(3);
    }

    private static async Task<HttpClient> AutenticarAsync(HttpClient client, string comerciante)
    {
        var resp = await client.PostAsJsonAsync("/auth/token", new { comercianteId = comerciante });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await resp.Content.ReadFromJsonAsync<TokenResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Access_Token);
        return client;
    }

    private static async Task PostLancamentoAsync(HttpClient client, string comerciante, decimal valor, int tipo, DateOnly data)
    {
        var resp = await client.PostAsJsonAsync("/lancamentos", new
        {
            comercianteId = comerciante,
            valor,
            tipo,
            data = data.ToString("yyyy-MM-dd")
        });
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    private static async Task<SaldoResponse?> AguardarConsolidacaoAsync(
        HttpClient client, string comerciante, DateOnly data, Func<SaldoResponse, bool> esperado)
    {
        for (var tentativa = 0; tentativa < 40; tentativa++)
        {
            var resp = await client.GetAsync($"/consolidado/{comerciante}/{data:yyyy-MM-dd}");
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                var saldo = await resp.Content.ReadFromJsonAsync<SaldoResponse>(Json);
                if (saldo is not null && esperado(saldo))
                    return saldo;
            }

            await Task.Delay(500);
        }

        return null;
    }
}
