using RabbitMQ.Client;

namespace LogisticsHub.Messaging.RabbitMQ;

public interface IRabbitMqConnectionProvider : IAsyncDisposable
{
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
}
