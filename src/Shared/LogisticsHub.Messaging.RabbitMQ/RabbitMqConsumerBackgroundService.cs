using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LogisticsHub.Messaging.RabbitMQ;

public abstract class RabbitMqConsumerBackgroundService<TMessage> : BackgroundService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "RabbitMQ consumer for queue {QueueName} failed. Reconnecting after delay.",
                    queueName);
            }

            await Task.Delay(ReconnectDelay, stoppingToken);
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
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
                await ProcessMessageAsync(channel, eventArgs, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to process RabbitMQ message from queue {QueueName}.",
                    queueName);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        while (connection.IsOpen && channel.IsOpen && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(ReconnectDelay, stoppingToken);
        }

        logger.LogWarning(
            "RabbitMQ consumer for queue {QueueName} lost its connection or channel. Reconnecting.",
            queueName);
    }

    private async Task ProcessMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = JsonSerializer.Deserialize<TMessage>(
                eventArgs.Body.Span,
                JsonSerializerOptions);

            if (message is null)
            {
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
                return;
            }

            await HandleMessageAsync(message, cancellationToken);
            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
        catch
        {
            if (channel.IsOpen)
            {
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
            }

            throw;
        }
    }

    protected abstract Task HandleMessageAsync(
        TMessage message,
        CancellationToken cancellationToken);
}
