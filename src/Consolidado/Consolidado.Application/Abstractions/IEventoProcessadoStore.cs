namespace Consolidado.Application.Abstractions;

/// <summary>Controle de idempotência do consumidor de eventos.</summary>
public interface IEventoProcessadoStore
{
    Task<bool> JaProcessadoAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarcarComoProcessadoAsync(Guid eventId, DateTime processadoEmUtc, CancellationToken cancellationToken = default);
}
