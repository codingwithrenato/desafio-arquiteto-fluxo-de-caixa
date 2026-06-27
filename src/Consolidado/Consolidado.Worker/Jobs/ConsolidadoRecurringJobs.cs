using Consolidado.Application.Abstractions;
using Consolidado.Application.Consolidados.FecharDia;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Consolidado.Worker.Jobs;

/// <summary>
/// Jobs recorrentes executados pelo Hangfire (no Worker, isolado da API de leitura).
/// Hangfire injeta esta classe via DI e garante execução única no cluster por disparo.
/// </summary>
public sealed class ConsolidadoRecurringJobs(
    ISender sender,
    ISaldoDiarioRepository repository,
    IClock clock,
    ILogger<ConsolidadoRecurringJobs> logger)
{
    public const string FechamentoDiarioId = "fechamento-consolidado-diario";
    public const string ReconciliacaoId = "reconciliacao-consolidado";

    /// <summary>Fecha o consolidado do dia anterior (D-1). Disparado por cron no início do dia.</summary>
    public async Task FecharDiaAnteriorAsync()
    {
        var dia = clock.Today.AddDays(-1);
        logger.LogInformation("Iniciando fechamento diário de {Dia}.", dia);
        var resultado = await sender.Send(new FecharConsolidadoDiarioCommand(dia));
        logger.LogInformation("Fechamento diário de {Dia} concluído: {Qtd} consolidados.",
            dia, resultado.IsSuccess ? resultado.Value : 0);
    }

    /// <summary>
    /// Reconciliação periódica: registra um panorama do consolidado do dia corrente
    /// (quantidade de comerciantes, total de lançamentos e saldo agregado). Serve como
    /// trilha de auditoria e ponto de detecção de divergências.
    /// </summary>
    public async Task ReconciliarAsync()
    {
        var hoje = clock.Today;
        var saldos = await repository.GetPorDataAsync(hoje);
        var totalLancamentos = saldos.Sum(s => s.QuantidadeLancamentos);
        var saldoAgregado = saldos.Sum(s => s.Saldo);

        logger.LogInformation(
            "Reconciliação de {Dia}: {Comerciantes} comerciantes, {Lancamentos} lançamentos, saldo agregado {Saldo}.",
            hoje, saldos.Count, totalLancamentos, saldoAgregado);
    }
}
