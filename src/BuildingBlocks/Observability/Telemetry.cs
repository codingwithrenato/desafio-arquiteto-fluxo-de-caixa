using System.Diagnostics;

namespace BuildingBlocks.Observability;

/// <summary>
/// Ponto único da instrumentação manual (spans de aplicação). A mesma ActivitySource é
/// usada pelo publisher (Outbox) e pelo consumer, e registrada no tracer via AddObservability.
/// </summary>
public static class Telemetry
{
    public const string ActivitySourceName = "FluxoDeCaixa";

    public static readonly ActivitySource Source = new(ActivitySourceName);
}
