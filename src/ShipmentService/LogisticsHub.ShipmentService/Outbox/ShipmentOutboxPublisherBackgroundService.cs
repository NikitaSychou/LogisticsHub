using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.Messaging.RabbitMQ;
using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Domain.Entities;
using System.Text.Json;

namespace LogisticsHub.ShipmentService.Outbox;

public sealed class ShipmentOutboxPublisherBackgroundService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(15);
    private const int BatchSize = 20;
    private const int MaxRetryCount = 5;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ShipmentOutboxPublisherBackgroundService> _logger;
    private readonly string _instanceId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    public ShipmentOutboxPublisherBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ShipmentOutboxPublisherBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedAny = await ProcessBatchAsync(stoppingToken);

                if (!processedAny)
                {
                    await Task.Delay(PollingInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to process shipment outbox messages.");
                await Task.Delay(PollingInterval, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<IShipmentDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

        var lockedAtUtc = DateTime.UtcNow;
        var messages = await dbContext.ClaimShipmentOutboxMessagesAsync(
            BatchSize,
            _instanceId,
            lockedAtUtc,
            LockTimeout,
            cancellationToken);

        if (messages.Count == 0)
        {
            _logger.LogDebug(
                "Shipment outbox publisher {PublisherInstanceId} found no claimable messages.",
                _instanceId);
        }
        else
        {
            _logger.LogInformation(
                "Shipment outbox publisher {PublisherInstanceId} claimed {MessageCount} messages.",
                _instanceId,
                messages.Count);
        }

        var hadFailure = false;

        foreach (var message in messages)
        {
            try
            {
                await PublishAsync(publisher, message, cancellationToken);

                message.ProcessedAtUtc = DateTime.UtcNow;
                message.LockedBy = null;
                message.LockedAtUtc = null;
                message.NextAttemptAtUtc = null;
                message.Error = null;

                _logger.LogDebug(
                    "Shipment outbox publisher {PublisherInstanceId} published message {OutboxMessageId}.",
                    _instanceId,
                    message.Id);
            }
            catch (Exception exception)
            {
                hadFailure = true;
                var failedAtUtc = DateTime.UtcNow;
                message.RetryCount++;
                message.LockedBy = null;
                message.LockedAtUtc = null;
                message.Error = exception.Message;

                if (message.RetryCount >= MaxRetryCount)
                {
                    message.FailedAtUtc = failedAtUtc;
                    message.NextAttemptAtUtc = null;

                    _logger.LogError(
                        exception,
                        "Shipment outbox message {OutboxMessageId} reached max retry count {RetryCount} and was marked failed.",
                        message.Id,
                        message.RetryCount);
                }
                else
                {
                    var retryDelay = GetRetryDelay(message.RetryCount);
                    message.NextAttemptAtUtc = failedAtUtc.Add(retryDelay);

                    _logger.LogWarning(
                        exception,
                        "Shipment outbox message {OutboxMessageId} failed on retry {RetryCount}. Next attempt scheduled at {NextAttemptAtUtc}.",
                        message.Id,
                        message.RetryCount,
                        message.NextAttemptAtUtc);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (hadFailure)
        {
            await Task.Delay(PollingInterval, cancellationToken);
        }

        return messages.Count > 0;
    }

    private static async Task PublishAsync(
        IRabbitMqPublisher publisher,
        ShipmentOutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Type == typeof(StockReservationRequestedIntegrationEvent).FullName)
        {
            var integrationEvent = JsonSerializer.Deserialize<StockReservationRequestedIntegrationEvent>(
                message.Payload,
                JsonSerializerOptions);

            if (integrationEvent is null)
            {
                throw new InvalidOperationException(
                    $"Unable to deserialize outbox message '{message.Id}' as {message.Type}.");
            }

            await publisher.PublishAsync(message.RoutingKey, integrationEvent, cancellationToken);
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported shipment outbox message type '{message.Type}'.");
    }

    private static TimeSpan GetRetryDelay(int retryCount)
    {
        var seconds = retryCount switch
        {
            1 => 30,
            2 => 60,
            3 => 120,
            4 => 300,
            _ => (int)MaxRetryDelay.TotalSeconds
        };

        return TimeSpan.FromSeconds(seconds);
    }
}
