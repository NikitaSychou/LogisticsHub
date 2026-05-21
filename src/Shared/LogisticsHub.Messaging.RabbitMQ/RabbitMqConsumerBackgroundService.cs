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

    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqOptions _options;
    private readonly ILogger _logger;
    private readonly string _queueName;
    private readonly string _routingKey;

    protected RabbitMqConsumerBackgroundService(
        IRabbitMqConnectionProvider connectionProvider,
        RabbitMqOptions options,
        ILogger logger,
        string queueName,
        string routingKey)
    {
        _connectionProvider = connectionProvider;
        _options = options;
        _logger = logger;
        _queueName = queueName;
        _routingKey = routingKey;
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
                _logger.LogError(
                    exception,
                    "RabbitMQ consumer for queue {QueueName} failed. Reconnecting after delay.",
                    _queueName);
            }

            await Task.Delay(ReconnectDelay, stoppingToken);
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var connection = await _connectionProvider.GetConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: _queueName,
            exchange: _options.ExchangeName,
            routingKey: _routingKey,
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
                _logger.LogError(
                    exception,
                    "Failed to process RabbitMQ message from queue {QueueName}.",
                    _queueName);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        while (connection.IsOpen && channel.IsOpen && !stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(ReconnectDelay, stoppingToken);
        }

        _logger.LogWarning(
            "RabbitMQ consumer for queue {QueueName} lost its connection or channel. Reconnecting.",
            _queueName);
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
