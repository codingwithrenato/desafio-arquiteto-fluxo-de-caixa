using Consolidado.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace IntegrationTests;

/// <summary>
/// Sobe a infraestrutura real (PostgreSQL x2, RabbitMQ, Redis) via Testcontainers e
/// instancia os dois serviços em processo:
/// - Lançamentos.API (com o OutboxDispatcher ativo);
/// - Consolidado.API + o consumidor da fila (mesmo host, para o teste).
/// Permite exercitar o fluxo assíncrono ponta a ponta de forma fidedigna.
/// </summary>
public sealed class FluxoDeCaixaFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _lancamentosDb = new PostgreSqlBuilder()
        .WithDatabase("lancamentos").WithUsername("postgres").WithPassword("postgres").Build();

    private readonly PostgreSqlContainer _consolidadoDb = new PostgreSqlBuilder()
        .WithDatabase("consolidado").WithUsername("postgres").WithPassword("postgres").Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder()
        .WithUsername("guest").WithPassword("guest").Build();

    private readonly RedisContainer _redis = new RedisBuilder().Build();

    public WebApplicationFactory<Lancamentos.API.ApiMarker> Lancamentos { get; private set; } = default!;
    public WebApplicationFactory<Consolidado.API.ApiMarker> Consolidado { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _lancamentosDb.StartAsync(),
            _consolidadoDb.StartAsync(),
            _rabbit.StartAsync(),
            _redis.StartAsync());

        var rabbitHost = _rabbit.Hostname;
        var rabbitPort = _rabbit.GetMappedPublicPort(5672);

        Lancamentos = new WebApplicationFactory<Lancamentos.API.ApiMarker>()
            .WithWebHostBuilder(builder => builder
                .UseSetting("ConnectionStrings:LancamentosDb", _lancamentosDb.GetConnectionString())
                .UseSetting("RabbitMq:HostName", rabbitHost)
                .UseSetting("RabbitMq:Port", rabbitPort.ToString())
                .UseSetting("RabbitMq:UserName", "guest")
                .UseSetting("RabbitMq:Password", "guest")
                .UseSetting("Outbox:PollingIntervalSeconds", "1")
                .UseSetting("Swagger:Enabled", "false"));

        Consolidado = new WebApplicationFactory<Consolidado.API.ApiMarker>()
            .WithWebHostBuilder(builder => builder
                .UseSetting("ConnectionStrings:ConsolidadoDb", _consolidadoDb.GetConnectionString())
                .UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString())
                .UseSetting("RabbitMq:HostName", rabbitHost)
                .UseSetting("RabbitMq:Port", rabbitPort.ToString())
                .UseSetting("RabbitMq:UserName", "guest")
                .UseSetting("RabbitMq:Password", "guest")
                .UseSetting("Swagger:Enabled", "false")
                // No teste, o mesmo host também consome a fila (declara fila/binding).
                .ConfigureServices(services => services.AddConsumidorDeLancamentos()));

        // Inicia o consumidor PRIMEIRO para que a fila/binding existam antes da publicação.
        _ = Consolidado.CreateClient();
        _ = Lancamentos.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await Lancamentos.DisposeAsync();
        await Consolidado.DisposeAsync();
        await Task.WhenAll(
            _lancamentosDb.DisposeAsync().AsTask(),
            _consolidadoDb.DisposeAsync().AsTask(),
            _rabbit.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask());
    }
}
