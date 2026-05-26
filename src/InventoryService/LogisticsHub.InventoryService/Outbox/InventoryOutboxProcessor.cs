using System.Text.Json;
using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;
using LogisticsHub.Messaging.RabbitMQ;

namespace LogisticsHub.InventoryService.Outbox;

public sealed class InventoryOutboxProcessor
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(15);
    private const int MaxRetryCount = 5;

    private readonly IInventoryDbContext _dbContext;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<InventoryOutboxProcessor> _logger;

    public InventoryOutboxProcessor(
        IInventoryDbContext dbContext,
        IRabbitMqPublisher publisher,
        ILogger<InventoryOutboxProcessor> logger)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<InventoryOutboxProcessBatchResult> ProcessBatchAsync(
        int batchSize,
        string instanceId,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken)
    {
        var lockedAtUtc = DateTime.UtcNow;
        var messages = await _dbContext.ClaimInventoryOutboxMessagesAsync(
            batchSize,
            instanceId,
            lockedAtUtc,
            lockTimeout,
            cancellationToken);

        if (messages.Count == 0)
        {
            _logger.LogDebug(
                "Inventory outbox publisher {PublisherInstanceId} found no claimable messages.",
                instanceId);
        }
        else
        {
            _logger.LogInformation(
                "Inventory outbox publisher {PublisherInstanceId} claimed {MessageCount} messages.",
                instanceId,
                messages.Count);
        }

        var hadFailure = false;

        foreach (var message in messages)
        {
            try
            {
                await PublishAsync(message, cancellationToken);

                message.ProcessedAtUtc = DateTime.UtcNow;
                message.LockedBy = null;
                message.LockedAtUtc = null;
                message.NextAttemptAtUtc = null;
                message.Error = null;

                _logger.LogDebug(
                    "Inventory outbox publisher {PublisherInstanceId} published message {OutboxMessageId}.",
                    instanceId,
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
                        "Inventory outbox message {OutboxMessageId} reached max retry count {RetryCount} and was marked failed.",
                        message.Id,
                        message.RetryCount);
                }
                else
                {
                    var retryDelay = GetRetryDelay(message.RetryCount);
                    message.NextAttemptAtUtc = failedAtUtc.Add(retryDelay);

                    _logger.LogWarning(
                        exception,
                        "Inventory outbox message {OutboxMessageId} failed on retry {RetryCount}. Next attempt scheduled at {NextAttemptAtUtc}.",
                        message.Id,
                        message.RetryCount,
                        message.NextAttemptAtUtc);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new InventoryOutboxProcessBatchResult(messages.Count > 0, hadFailure);
    }

    private async Task PublishAsync(
        InventoryOutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Type == typeof(StockReservedIntegrationEvent).FullName)
        {
            var integrationEvent = JsonSerializer.Deserialize<StockReservedIntegrationEvent>(
                message.Payload,
                JsonSerializerOptions);

            if (integrationEvent is null)
            {
                throw new InvalidOperationException(
                    $"Unable to deserialize outbox message '{message.Id}' as {message.Type}.");
            }

            await _publisher.PublishAsync(message.RoutingKey, integrationEvent, cancellationToken);
            return;
        }

        if (message.Type == typeof(StockReservationFailedIntegrationEvent).FullName)
        {
            var integrationEvent = JsonSerializer.Deserialize<StockReservationFailedIntegrationEvent>(
                message.Payload,
                JsonSerializerOptions);

            if (integrationEvent is null)
            {
                throw new InvalidOperationException(
                    $"Unable to deserialize outbox message '{message.Id}' as {message.Type}.");
            }

            await _publisher.PublishAsync(message.RoutingKey, integrationEvent, cancellationToken);
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported inventory outbox message type '{message.Type}'.");
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

public sealed record InventoryOutboxProcessBatchResult(bool ProcessedAny, bool HadFailure);
