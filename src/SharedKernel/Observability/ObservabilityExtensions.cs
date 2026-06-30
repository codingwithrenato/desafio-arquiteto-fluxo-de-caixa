using BuildingBlocks.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SharedKernel.Observability;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Configura tracing distribuído com OpenTelemetry (vendor-neutral). Instrumenta ASP.NET
    /// Core, HttpClient e Npgsql, mais a ActivitySource da aplicação (spans de Outbox/consumo).
    /// Exporta via OTLP quando <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> está definido (ex.: Jaeger em
    /// dev; Datadog/New Relic/Grafana em produção — só muda o endpoint, sem mudar código).
    /// </summary>
    public static IServiceCollection AddObservability(
        this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Npgsql")                       // spans de banco (Npgsql 8)
                    .AddSource(Telemetry.ActivitySourceName);  // spans da aplicação (Outbox/consumer)

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
            });

        return services;
    }
}
