using System.Text.Json;
using Consolidado.Application.Abstractions;
using Consolidado.Application.Consolidados;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Consolidado.Infrastructure.Caching;

/// <summary>
/// Cache distribuído do saldo consolidado sobre Redis (IDistributedCache).
/// Falhas no cache NÃO derrubam a leitura: em erro, o serviço degrada para o banco
/// (resiliência — o Redis é um acelerador, não um ponto único de falha).
/// </summary>
public sealed class RedisSaldoConsolidadoCache(
    IDistributedCache cache,
    ILogger<RedisSaldoConsolidadoCache> logger) : ISaldoConsolidadoCache
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions EntryOptions = new()
    {
        // TTL curto: a invalidação na escrita mantém o dado fresco; o TTL é só uma rede de segurança.
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    private static string Key(string comercianteId, DateOnly data) => $"consolidado:{comercianteId}:{data:yyyy-MM-dd}";

    public async Task<SaldoConsolidadoDto?> GetAsync(string comercianteId, DateOnly data, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await cache.GetAsync(Key(comercianteId, data), cancellationToken);
            return bytes is null ? null : JsonSerializer.Deserialize<SaldoConsolidadoDto>(bytes, Json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao ler do cache; seguindo para o banco.");
            return null;
        }
    }

    public async Task SetAsync(SaldoConsolidadoDto saldo, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(saldo, Json);
            await cache.SetAsync(Key(saldo.ComercianteId, saldo.Data), bytes, EntryOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao gravar no cache (ignorado).");
        }
    }

    public async Task RemoveAsync(string comercianteId, DateOnly data, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(Key(comercianteId, data), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao invalidar o cache (ignorado).");
        }
    }
}
