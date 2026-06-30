using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace BuildingBlocks.Observability;

/// <summary>
/// Injeta/extrai o contexto de trace (W3C) nos headers da mensagem RabbitMQ, costurando o
/// span do publisher ao span do consumer através da fronteira assíncrona do broker.
/// </summary>
public static class TraceContextPropagation
{
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    /// <summary>Injeta o contexto da activity nos headers (chave/valor string).</summary>
    public static void Inject(Activity? activity, IDictionary<string, object> headers)
    {
        if (activity is null) return;
        var context = new PropagationContext(activity.Context, Baggage.Current);
        Propagator.Inject(context, headers, static (carrier, key, value) => carrier[key] = value);
    }

    /// <summary>Extrai o contexto dos headers da mensagem (valores podem vir como byte[]).</summary>
    public static PropagationContext Extract(IDictionary<string, object>? headers)
        => Propagator.Extract(default, headers ?? new Dictionary<string, object>(), static (carrier, key) =>
        {
            if (carrier is null || !carrier.TryGetValue(key, out var value))
                return Array.Empty<string>();

            return value switch
            {
                byte[] bytes => [Encoding.UTF8.GetString(bytes)],
                string s => [s],
                _ => Array.Empty<string>()
            };
        });
}
