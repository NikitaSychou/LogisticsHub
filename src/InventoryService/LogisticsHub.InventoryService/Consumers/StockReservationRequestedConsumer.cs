using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.InventoryService.Application.StockReservations;
using LogisticsHub.Messaging.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;

namespace LogisticsHub.InventoryService.Consumers;

public sealed class StockReservationRequestedConsumer
    : RabbitMqConsumerBackgroundService<StockReservationRequestedIntegrationEvent>
{
    private const string QueueName = "inventory.stock-reservation.requested";

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<StockReservationRequestedConsumer> logger;

    public StockReservationRequestedConsumer(
        IRabbitMqConnectionProvider connectionProvider,
        RabbitMqOptions options,
        ILogger<StockReservationRequestedConsumer> logger,
        IServiceScopeFactory serviceScopeFactory)
        : base(
            connectionProvider,
            options,
            logger,
            QueueName,
            StockReservationRoutingKeys.Requested)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    protected override async Task HandleMessageAsync(
        StockReservationRequestedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();

        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

        if (message.EventId == Guid.Empty)
        {
            await PublishFailedAsync(publisher, message.ShipmentId, "Event ID is required.", cancellationToken);
            return;
        }

        if (message.ShipmentId == Guid.Empty)
        {
            await PublishFailedAsync(publisher, message.ShipmentId, "Shipment ID is required.", cancellationToken);
            return;
        }

        if (message.Items.Count == 0)
        {
            await PublishFailedAsync(publisher, message.ShipmentId, "At least one item is required.", cancellationToken);
            return;
        }

        var skus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in message.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Sku))
            {
                await PublishFailedAsync(publisher, message.ShipmentId, "SKU is required.", cancellationToken);
                return;
            }

            if (!skus.Add(item.Sku.Trim()))
            {
                await PublishFailedAsync(publisher, message.ShipmentId, $"Duplicate SKU '{item.Sku}' is not allowed.", cancellationToken);
                return;
            }

            if (item.Quantity <= 0)
            {
                await PublishFailedAsync(publisher, message.ShipmentId, "Quantity must be greater than zero.", cancellationToken);
                return;
            }
        }

        var createStockReservation = scope.ServiceProvider.GetRequiredService<CreateStockReservation>();
        var command = new CreateStockReservationCommand(
            message.ShipmentId,
            message.Items
                .Select(item => new StockReservationItemCommand(item.Sku.Trim(), item.Quantity))
                .ToArray(),
            message.EventId);

        var result = await createStockReservation.ExecuteAsync(command, cancellationToken);

        if (result.AlreadyProcessed)
        {
            logger.LogInformation(
                "Ignoring duplicate stock reservation request event {EventId} for shipment {ShipmentId}.",
                message.EventId,
                message.ShipmentId);

            return;
        }

        if (result.Reservation is null)
        {
            await PublishFailedAsync(
                publisher,
                message.ShipmentId,
                result.FailureReason ?? "Stock reservation failed.",
                cancellationToken);

            return;
        }

        await publisher.PublishAsync(
            StockReservationRoutingKeys.Reserved,
            new StockReservedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                result.Reservation.ShipmentId,
                result.Reservation.ReservationId),
            cancellationToken);
    }

    private static async Task PublishFailedAsync(
        IRabbitMqPublisher publisher,
        Guid shipmentId,
        string reason,
        CancellationToken cancellationToken)
    {
        await publisher.PublishAsync(
            StockReservationRoutingKeys.Failed,
            new StockReservationFailedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                shipmentId,
                reason),
            cancellationToken);
    }
}
