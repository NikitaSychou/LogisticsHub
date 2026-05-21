using System.Text.Json;
using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;
using LogisticsHub.Messaging.RabbitMQ;

namespace LogisticsHub.InventoryService.Outbox;

public sealed class InventoryOutboxPublisherBackgroundService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<InventoryOutboxPublisherBackgroundService> _logger;

    public InventoryOutboxPublisherBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<InventoryOutboxPublisherBackgroundService> logger)
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
                _logger.LogError(exception, "Failed to process inventory outbox messages.");
                await Task.Delay(PollingInterval, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<IInventoryDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

        var messages = await dbContext.GetUnprocessedInventoryOutboxMessagesAsync(
            BatchSize,
            cancellationToken);

        var hadFailure = false;

        foreach (var message in messages)
        {
            try
            {
                await PublishAsync(publisher, message, cancellationToken);

                message.ProcessedAtUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception exception)
            {
                hadFailure = true;
                message.RetryCount++;
                message.Error = exception.Message;

                _logger.LogError(
                    exception,
                    "Failed to publish inventory outbox message {OutboxMessageId}.",
                    message.Id);
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

            await publisher.PublishAsync(message.RoutingKey, integrationEvent, cancellationToken);
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

            await publisher.PublishAsync(message.RoutingKey, integrationEvent, cancellationToken);
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported inventory outbox message type '{message.Type}'.");
    }
}
