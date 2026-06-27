namespace Consolidado.Application.Consolidados;

/// <summary>Representação de leitura do saldo consolidado diário (servida ao cliente e cacheada).</summary>
public sealed record SaldoConsolidadoDto(
    string ComercianteId,
    DateOnly Data,
    decimal TotalCreditos,
    decimal TotalDebitos,
    decimal Saldo,
    int QuantidadeLancamentos,
    DateTime AtualizadoEmUtc,
    bool Fechado);
