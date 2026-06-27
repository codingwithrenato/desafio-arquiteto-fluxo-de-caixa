using Lancamentos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lancamentos.API.Extensions;

public static class MigrationExtensions
{
    /// <summary>
    /// Aplica as migrations pendentes na subida. Conveniência para o ambiente local
    /// (docker-compose). Em produção, migrations rodam num passo de deploy dedicado.
    /// </summary>
    public static async Task MigrateLancamentosDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LancamentosDbContext>();
        await db.Database.MigrateAsync();
    }
}
