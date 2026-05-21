using System.Text.Json;
using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Domain.Entities;
using LogisticsHub.ShipmentService.Domain.Enums;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed class CreateShipment
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IShipmentDbContext dbContext;

    public CreateShipment(IShipmentDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<CreateShipmentResult> ExecuteAsync(
        CreateShipmentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = DateTime.UtcNow;
        var shipmentId = Guid.NewGuid();
        var shipmentNumber = $"SHP-{now:yyyyMMddHHmmssfff}-{shipmentId.ToString("N")[..8].ToUpperInvariant()}";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.ReservationRequested,
            ShipmentNumber = shipmentNumber,
            CreatedAt = now,
            UpdatedAt = now
        };

        await dbContext.AddShipmentAsync(shipment, cancellationToken);

        foreach (var item in command.Items)
        {
            var shipmentItem = new ShipmentItem
            {
                ShipmentId = shipment.Id,
                Sku = item.Sku,
                Quantity = item.Quantity
            };

            await dbContext.AddShipmentItemAsync(shipmentItem, cancellationToken);
        }

        var stockReservationRequested = new StockReservationRequestedIntegrationEvent(
            Guid.NewGuid(),
            now,
            shipment.Id,
            command.Items
                .Select(item => new StockReservationRequestedItem(item.Sku, item.Quantity))
                .ToArray());

        var outboxMessage = new ShipmentOutboxMessage
        {
            Id = stockReservationRequested.EventId,
            OccurredAtUtc = stockReservationRequested.OccurredAtUtc,
            Type = typeof(StockReservationRequestedIntegrationEvent).FullName!,
            RoutingKey = StockReservationRoutingKeys.Requested,
            Payload = JsonSerializer.Serialize(stockReservationRequested, JsonSerializerOptions),
            CreatedAtUtc = now
        };

        await dbContext.AddShipmentOutboxMessageAsync(outboxMessage, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateShipmentResult(shipment.Id, shipment.Status);
    }
}
