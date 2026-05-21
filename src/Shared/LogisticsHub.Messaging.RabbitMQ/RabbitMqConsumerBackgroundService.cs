using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LogisticsHub.Messaging.RabbitMQ;

public abstract class RabbitMqConsumerBackgroundService<TMessage> : BackgroundService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqConnectionProvider connectionProvider;
    private readonly RabbitMqOptions options;
    private readonly ILogger logger;
    private readonly string queueName;
    private readonly string routingKey;

    protected RabbitMqConsumerBackgroundService(
        IRabbitMqConnectionProvider connectionProvider,
        RabbitMqOptions options,
        ILogger logger,
        string queueName,
        string routingKey)
    {
        this.connectionProvider = connectionProvider;
        this.options = options;
        this.logger = logger;
        this.queueName = queueName;
        this.routingKey = routingKey;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: options.ExchangeName,
            routingKey: routingKey,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<TMessage>(
                    eventArgs.Body.Span,
                    JsonSerializerOptions);

                if (message is null)
                {
                    await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    return;
                }

                await HandleMessageAsync(message, stoppingToken);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to process RabbitMQ message from queue {QueueName}.", queueName);
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    protected abstract Task HandleMessageAsync(
        TMessage message,
        CancellationToken cancellationToken);
}
