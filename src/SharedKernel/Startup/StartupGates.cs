using Microsoft.Extensions.Logging;
using Npgsql;

namespace SharedKernel.Startup;

/// <summary>
/// Gates de readiness na subida das aplicações. Tornam o boot resiliente a dependências
/// que ainda estão inicializando — sem confiar apenas no healthcheck externo do orquestrador,
/// que pode reportar "pronto" um instante antes do serviço aceitar requisições de fato.
/// </summary>
public static class StartupGates
{
    /// <summary>
    /// Aguarda o PostgreSQL aceitar consultas (não só conexões TCP). Resolve a corrida em que
    /// o banco responde "starting up" (57P03) logo após o container ficar healthy.
    /// </summary>
    public static async Task WaitForPostgresAsync(
        string connectionString,
        ILogger logger,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var limite = timeout ?? TimeSpan.FromSeconds(90);
        var inicio = DateTime.UtcNow;
        var tentativa = 0;

        while (true)
        {
            tentativa++;
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new NpgsqlCommand("SELECT 1", conn);
                await cmd.ExecuteScalarAsync(cancellationToken);
                logger.LogInformation("PostgreSQL pronto (tentativa {Tentativa}).", tentativa);
                return;
            }
            catch (Exception ex)
            {
                if (DateTime.UtcNow - inicio > limite)
                    throw new TimeoutException(
                        $"PostgreSQL não ficou pronto em {limite.TotalSeconds}s.", ex);

                logger.LogWarning("PostgreSQL ainda indisponível (tentativa {Tentativa}): {Erro}. Retentando...",
                    tentativa, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }
}
