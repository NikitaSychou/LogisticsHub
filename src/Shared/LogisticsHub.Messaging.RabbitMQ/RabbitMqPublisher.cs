using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace LogisticsHub.Messaging.RabbitMQ;

public sealed class RabbitMqPublisher : IRabbitMqPublisher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqConnectionProvider connectionProvider;
    private readonly RabbitMqOptions options;

    public RabbitMqPublisher(
        IRabbitMqConnectionProvider connectionProvider,
        RabbitMqOptions options)
    {
        this.connectionProvider = connectionProvider;
        this.options = options;
    }

    public async Task PublishAsync<TMessage>(
        string routingKey,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        ArgumentNullException.ThrowIfNull(message);

        var connection = await connectionProvider.GetConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonSerializerOptions));
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(
            exchange: options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
