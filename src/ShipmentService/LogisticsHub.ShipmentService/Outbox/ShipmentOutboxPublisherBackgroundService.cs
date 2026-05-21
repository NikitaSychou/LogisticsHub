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
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ShipmentOutboxPublisherBackgroundService> _logger;

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

        var messages = await dbContext.GetUnprocessedShipmentOutboxMessagesAsync(
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
                    "Failed to publish shipment outbox message {OutboxMessageId}.",
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
}
