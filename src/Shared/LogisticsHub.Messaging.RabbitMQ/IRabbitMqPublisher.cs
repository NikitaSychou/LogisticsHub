namespace LogisticsHub.Messaging.RabbitMQ;

public interface IRabbitMqPublisher
{
    Task PublishAsync<TMessage>(
        string routingKey,
        TMessage message,
        CancellationToken cancellationToken = default);
}
