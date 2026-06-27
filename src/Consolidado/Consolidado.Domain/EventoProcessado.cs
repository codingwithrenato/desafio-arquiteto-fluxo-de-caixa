namespace Consolidado.Domain;

/// <summary>
/// Registro de idempotência. Guarda o Id de cada evento já consumido para que uma
/// reentrega (garantia at-least-once do broker) não contabilize o mesmo lançamento
/// duas vezes. Gravado na MESMA transação da atualização do saldo.
/// </summary>
public sealed class EventoProcessado
{
    public Guid EventId { get; private set; }
    public DateTime ProcessadoEmUtc { get; private set; }

    private EventoProcessado() { }

    public EventoProcessado(Guid eventId, DateTime processadoEmUtc)
    {
        EventId = eventId;
        ProcessadoEmUtc = processadoEmUtc;
    }
}
