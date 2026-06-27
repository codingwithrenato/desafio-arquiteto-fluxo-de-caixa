namespace Lancamentos.Infrastructure.Outbox;

/// <summary>
/// Linha da tabela de Outbox. Gravada na MESMA transação do lançamento, garante
/// que nenhum evento se perca caso o broker esteja indisponível no momento da escrita.
/// O <c>OutboxDispatcher</c> publica as mensagens pendentes e marca <see cref="ProcessadoEmUtc"/>.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    /// <summary>Nome lógico do tipo do evento (para diagnóstico/auditoria).</summary>
    public string Tipo { get; set; } = default!;

    /// <summary>Routing key usada para publicar no exchange.</summary>
    public string RoutingKey { get; set; } = default!;

    /// <summary>Payload do evento serializado em JSON.</summary>
    public string Conteudo { get; set; } = default!;

    public DateTime OccurredOnUtc { get; set; }

    /// <summary>Nulo enquanto pendente; preenchido quando publicado com sucesso.</summary>
    public DateTime? ProcessadoEmUtc { get; set; }

    /// <summary>Número de tentativas de publicação já realizadas.</summary>
    public int Tentativas { get; set; }

    /// <summary>Último erro de publicação, se houver.</summary>
    public string? UltimoErro { get; set; }
}
