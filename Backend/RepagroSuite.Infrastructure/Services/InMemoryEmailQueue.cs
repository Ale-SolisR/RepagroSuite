using System.Threading.Channels;
using RepagroSuite.Application.Common.Interfaces;

namespace RepagroSuite.Infrastructure.Services;

// Cola en memoria basada en System.Threading.Channels.
// Capacidad acotada (5000): si se llena, descartamos el más antiguo para no bloquear requests HTTP.
// Para alta carga real (>50k correos/día) migrar a Azure Service Bus / RabbitMQ.
public class InMemoryEmailQueue : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

    public void Enqueue(EmailMessage message)
    {
        // TryWrite es no bloqueante: si el canal está lleno, FullMode.DropOldest se ocupa.
        _channel.Writer.TryWrite(message);
    }

    public IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
