using Microsoft.Extensions.Hosting;
using Serilog;

namespace SharedKernel.Observability;

public static class SerilogConfiguration
{
    /// <summary>
    /// Configuração de logging compartilhada pelos serviços. Enriquece cada linha com o
    /// TraceId/SpanId da Activity atual, permitindo **correlacionar logs e traces**: pega-se um
    /// trace no Jaeger e encontram-se os logs daquele fluxo pelo mesmo trace_id (e vice-versa).
    /// </summary>
    public static void Configure(HostBuilderContext context, LoggerConfiguration config)
    {
        config
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.With(new ActivityEnricher())
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} trace_id={TraceId}{NewLine}{Exception}");
    }
}
