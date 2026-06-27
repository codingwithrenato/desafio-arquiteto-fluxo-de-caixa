using Consolidado.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.API.Extensions;

public static class MigrationExtensions
{
    /// <summary>
    /// Aplica as migrations do banco de Consolidado na subida. A API é o migrador
    /// único do schema da aplicação; o Worker apenas consome (evita corrida de migrations).
    /// </summary>
    public static async Task MigrateConsolidadoDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
        await db.Database.MigrateAsync();
    }
}
