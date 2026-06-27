using Consolidado.Application.Abstractions;
using Consolidado.Infrastructure.Caching;
using Consolidado.Infrastructure.Messaging;
using Consolidado.Infrastructure.Persistence;
using Consolidado.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Consolidado.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Núcleo de infraestrutura compartilhado pela API (read) e pelo Worker (write/consumo):
    /// persistência, cache e conexão de mensageria. NÃO inclui o consumidor — esse é
    /// registrado apenas pelo Worker (separação read-API / job-worker).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ConsolidadoDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ConsolidadoDb"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<ISaldoDiarioRepository, SaldoDiarioRepository>();
        services.AddScoped<IEventoProcessadoStore, EventoProcessadoStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();

        // Cache distribuído (Redis).
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis"));
        services.AddScoped<ISaldoConsolidadoCache, RedisSaldoConsolidadoCache>();

        // Conexão de mensageria (sem iniciar consumo aqui).
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();

        return services;
    }

    /// <summary>Registra o consumidor da fila. Chamado SOMENTE pelo Worker.</summary>
    public static IServiceCollection AddConsumidorDeLancamentos(this IServiceCollection services)
    {
        services.AddHostedService<LancamentoRegistradoConsumer>();
        return services;
    }
}
