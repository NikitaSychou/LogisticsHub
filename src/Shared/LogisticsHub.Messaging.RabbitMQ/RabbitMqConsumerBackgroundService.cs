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
    private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan SecondRetryDelay = TimeSpan.FromSeconds(5);
    private const int MaxProcessingAttempts = 3;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);

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

        var deadLetterExchangeName = $"{_options.ExchangeName}.dlx";
        var deadLetterQueueName = $"{_queueName}.dlq";
        var deadLetterRoutingKey = $"{_queueName}.dead-letter";

        await channel.ExchangeDeclareAsync(
            exchange: deadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: deadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: deadLetterQueueName,
            exchange: deadLetterExchangeName,
            routingKey: deadLetterRoutingKey,
            cancellationToken: stoppingToken);

        var queueArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = deadLetterExchangeName,
            ["x-dead-letter-routing-key"] = deadLetterRoutingKey
        };

        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
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

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: _options.ConsumerPrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

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
            TMessage? message;
            try
            {
                message = DeserializeMessage(eventArgs);
            }
            catch (JsonException exception)
            {
                _logger.LogError(
                    exception,
                    "RabbitMQ message from queue {QueueName} could not be deserialized as {MessageType}. Sending to DLQ.",
                    _queueName,
                    typeof(TMessage).Name);

                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
                return;
            }

            if (message is null)
            {
                _logger.LogError(
                    "RabbitMQ message from queue {QueueName} could not be deserialized as {MessageType}. Sending to DLQ.",
                    _queueName,
                    typeof(TMessage).Name);

                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
                return;
            }

            var processed = await TryHandleMessageAsync(message, eventArgs, cancellationToken);
            if (!processed)
            {
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
                return;
            }

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

    private static TMessage? DeserializeMessage(BasicDeliverEventArgs eventArgs)
    {
        return JsonSerializer.Deserialize<TMessage>(
            eventArgs.Body.Span,
            JsonSerializerOptions);
    }

    private async Task<bool> TryHandleMessageAsync(
        TMessage message,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxProcessingAttempts; attempt++)
        {
            try
            {
                await HandleMessageAsync(message, cancellationToken);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (attempt < MaxProcessingAttempts)
            {
                var retryDelay = GetRetryDelay(attempt);

                _logger.LogWarning(
                    exception,
                    "RabbitMQ message processing failed for queue {QueueName} on attempt {Attempt}/{MaxAttempts}. Retrying after {RetryDelay}. Delivery tag {DeliveryTag}.",
                    _queueName,
                    attempt,
                    MaxProcessingAttempts,
                    retryDelay,
                    eventArgs.DeliveryTag);

                await Task.Delay(retryDelay, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "RabbitMQ message processing failed for queue {QueueName} after {MaxAttempts} attempts. Sending to DLQ. Delivery tag {DeliveryTag}.",
                    _queueName,
                    MaxProcessingAttempts,
                    eventArgs.DeliveryTag);

                return false;
            }
        }

        return false;
    }

    private static TimeSpan GetRetryDelay(int failedAttempt)
    {
        return failedAttempt == 1
            ? FirstRetryDelay
            : SecondRetryDelay;
    }

    protected abstract Task HandleMessageAsync(
        TMessage message,
        CancellationToken cancellationToken);
}
