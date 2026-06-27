namespace Lancamentos.Infrastructure.Outbox;

/// <summary>Configuração do dispatcher de Outbox (Options pattern).</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>Intervalo entre varreduras da tabela de outbox.</summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>Quantidade máxima de mensagens publicadas por ciclo.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>Tentativas máximas antes de considerar a mensagem como "envenenada".</summary>
    public int MaxTentativas { get; set; } = 10;
}
