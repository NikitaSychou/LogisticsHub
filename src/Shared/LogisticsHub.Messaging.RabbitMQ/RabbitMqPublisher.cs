using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace LogisticsHub.Messaging.RabbitMQ;

public sealed class RabbitMqPublisher : IRabbitMqPublisher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(
        IRabbitMqConnectionProvider connectionProvider,
        RabbitMqOptions options,
        ILogger<RabbitMqPublisher> logger)
    {
        _connectionProvider = connectionProvider;
        _options = options;
        _logger = logger;
    }

    public async Task PublishAsync<TMessage>(
        string routingKey,
        TMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);
        ArgumentNullException.ThrowIfNull(message);

        var messageType = typeof(TMessage).Name;
        var messageId = GetMessageId(message);

        try
        {
            _logger.LogDebug(
                "Publishing RabbitMQ message {MessageType} with id {MessageId} to exchange {ExchangeName} using routing key {RoutingKey}.",
                messageType,
                messageId,
                _options.ExchangeName,
                routingKey);

            var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Declaring RabbitMQ exchange {ExchangeName} before publishing message {MessageType} with id {MessageId}.",
                _options.ExchangeName,
                messageType,
                messageId);

            await channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
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
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Published RabbitMQ message {MessageType} with id {MessageId} to exchange {ExchangeName} using routing key {RoutingKey}.",
                messageType,
                messageId,
                _options.ExchangeName,
                routingKey);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish RabbitMQ message {MessageType} with id {MessageId} to exchange {ExchangeName} using routing key {RoutingKey}.",
                messageType,
                messageId,
                _options.ExchangeName,
                routingKey);

            throw;
        }
    }

    private static object? GetMessageId<TMessage>(TMessage message)
    {
        var messageType = typeof(TMessage);
        var eventIdProperty = messageType.GetProperty("EventId");
        if (eventIdProperty is not null)
        {
            return eventIdProperty.GetValue(message);
        }

        var idProperty = messageType.GetProperty("Id");
        return idProperty?.GetValue(message);
    }
}
