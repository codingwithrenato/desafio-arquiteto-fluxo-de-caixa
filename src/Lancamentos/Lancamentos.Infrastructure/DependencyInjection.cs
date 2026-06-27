using Lancamentos.Application.Abstractions;
using Lancamentos.Infrastructure.Messaging;
using Lancamentos.Infrastructure.Outbox;
using Lancamentos.Infrastructure.Persistence;
using Lancamentos.Infrastructure.Time;
using BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lancamentos.Infrastructure;

/// <summary>Composição de dependências da Infraestrutura do serviço de Lançamentos.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Persistência (database-per-service) com convenção snake_case do PostgreSQL.
        services.AddDbContext<LancamentosDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("LancamentosDb"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<ILancamentoRepository, LancamentoRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutbox, OutboxWriter>();
        services.AddSingleton<IClock, SystemClock>();

        // Mensageria (RabbitMQ).
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();

        // O mesmo publisher atende a porta tipada (IEventPublisher) e a de baixo nível
        // (IMessageBusPublisher) usada pelo dispatcher — registrado uma única vez.
        services.AddSingleton<RabbitMqPublisher>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<RabbitMqPublisher>());
        services.AddSingleton<IMessageBusPublisher>(sp => sp.GetRequiredService<RabbitMqPublisher>());

        // Outbox dispatcher.
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.AddHostedService<OutboxDispatcher>();

        return services;
    }
}
