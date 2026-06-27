namespace Consolidado.Domain;

/// <summary>
/// Projeção (read model) do saldo consolidado de um comerciante em um dia.
/// É construída de forma incremental a partir dos eventos de lançamento — por isso
/// a leitura do consolidado é O(1) e suporta o pico de 50 req/s sem recalcular nada.
/// </summary>
public sealed class SaldoDiario
{
    public Guid Id { get; private set; }
    public string ComercianteId { get; private set; } = default!;
    public DateOnly Data { get; private set; }
    public decimal TotalCreditos { get; private set; }
    public decimal TotalDebitos { get; private set; }
    public int QuantidadeLancamentos { get; private set; }
    public DateTime AtualizadoEmUtc { get; private set; }
    public bool Fechado { get; private set; }

    /// <summary>Saldo do dia = créditos − débitos. Calculado, não persistido.</summary>
    public decimal Saldo => TotalCreditos - TotalDebitos;

    private SaldoDiario() { }

    public static SaldoDiario Criar(string comercianteId, DateOnly data, DateTime agoraUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            ComercianteId = comercianteId,
            Data = data,
            TotalCreditos = 0m,
            TotalDebitos = 0m,
            QuantidadeLancamentos = 0,
            AtualizadoEmUtc = agoraUtc,
            Fechado = false
        };

    public void AplicarCredito(decimal valor, DateTime agoraUtc)
    {
        GarantirAberto();
        TotalCreditos += valor;
        Contabilizar(agoraUtc);
    }

    public void AplicarDebito(decimal valor, DateTime agoraUtc)
    {
        GarantirAberto();
        TotalDebitos += valor;
        Contabilizar(agoraUtc);
    }

    /// <summary>Fecha o dia (fechamento diário). Após fechado, não aceita novos lançamentos.</summary>
    public void Fechar(DateTime agoraUtc)
    {
        Fechado = true;
        AtualizadoEmUtc = agoraUtc;
    }

    private void Contabilizar(DateTime agoraUtc)
    {
        QuantidadeLancamentos++;
        AtualizadoEmUtc = agoraUtc;
    }

    private void GarantirAberto()
    {
        if (Fechado)
            throw new InvalidOperationException(
                $"O consolidado de {ComercianteId} em {Data} já está fechado e não aceita novos lançamentos.");
    }
}
