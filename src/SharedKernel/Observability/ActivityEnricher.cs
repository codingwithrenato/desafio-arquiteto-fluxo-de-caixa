using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace SharedKernel.Observability;

/// <summary>
/// Enriquecedor do Serilog que adiciona TraceId/SpanId da Activity atual a cada log,
/// correlacionando logs e traces (mesmo trace_id no Jaeger e nos logs).
/// </summary>
public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null) return;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));
    }
}
