using Lancamentos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Startup;

namespace Lancamentos.API.Extensions;

public static class MigrationExtensions
{
    /// <summary>
    /// Aplica as migrations pendentes na subida. Conveniência para o ambiente local
    /// (docker-compose). Em produção, migrations rodam num passo de deploy dedicado.
    /// Espera o PostgreSQL aceitar consultas antes de migrar (resiliência de boot).
    /// </summary>
    public static async Task MigrateLancamentosDatabaseAsync(this WebApplication app)
    {
        await StartupGates.WaitForPostgresAsync(
            app.Configuration.GetConnectionString("LancamentosDb")!, app.Logger);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
        await db.Database.MigrateAsync();
    }
}
