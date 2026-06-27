using Consolidado.Application.Abstractions;
using Consolidado.Domain;
using Microsoft.EntityFrameworkCore;

namespace Consolidado.Infrastructure.Persistence;

public sealed class EventoProcessadoStore(ConsolidadoDbContext context) : IEventoProcessadoStore
{
    public Task<bool> JaProcessadoAsync(Guid eventId, CancellationToken cancellationToken = default)
        => context.EventosProcessados.AnyAsync(x => x.EventId == eventId, cancellationToken);

    public async Task MarcarComoProcessadoAsync(Guid eventId, DateTime processadoEmUtc, CancellationToken cancellationToken = default)
        => await context.EventosProcessados.AddAsync(new EventoProcessado(eventId, processadoEmUtc), cancellationToken);
}
