namespace Consolidado.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    /// <summary>Quantidade de mensagens entregues sem ack antes de pausar (controle de vazão).</summary>
    public ushort PrefetchCount { get; set; } = 20;
}
