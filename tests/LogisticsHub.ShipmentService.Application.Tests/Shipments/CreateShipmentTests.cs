using System.Text.Json;
using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Application.Tests.Fakes;
using LogisticsHub.ShipmentService.Domain.Enums;
using Xunit;

namespace LogisticsHub.ShipmentService.Application.Tests.Shipments;

public sealed class CreateShipmentTests
{
    [Fact]
    public async Task Handle_CreatesShipmentAndStockReservationRequestedOutboxMessage()
    {
        // Arrange
        var dbContext = new FakeShipmentDbContext();
        var command = new CreateShipmentCommand(
            [
                new CreateShipmentItemCommand("TEST-SKU-001", 5),
                new CreateShipmentItemCommand("TEST-SKU-002", 3)
            ]);
        var handler = new CreateShipment(dbContext);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var shipment = Assert.Single(dbContext.Shipments);
        Assert.Equal(result.ShipmentId, shipment.Id);
        Assert.Equal(ShipmentStatus.ReservationRequested, shipment.Status);
        Assert.Equal(ShipmentStatus.ReservationRequested, result.Status);

        Assert.Equal(2, dbContext.ShipmentItems.Count);
        Assert.All(dbContext.ShipmentItems, item => Assert.Equal(shipment.Id, item.ShipmentId));
        Assert.Contains(dbContext.ShipmentItems, item => item.Sku == "TEST-SKU-001" && item.Quantity == 5);
        Assert.Contains(dbContext.ShipmentItems, item => item.Sku == "TEST-SKU-002" && item.Quantity == 3);

        var outboxMessage = Assert.Single(dbContext.OutboxMessages);
        Assert.Equal(typeof(StockReservationRequestedIntegrationEvent).FullName, outboxMessage.Type);
        Assert.Equal(StockReservationRoutingKeys.Requested, outboxMessage.RoutingKey);

        var integrationEvent = JsonSerializer.Deserialize<StockReservationRequestedIntegrationEvent>(
            outboxMessage.Payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(integrationEvent);
        Assert.Equal(shipment.Id, integrationEvent.ShipmentId);
        Assert.Equal(2, integrationEvent.Items.Count);
        Assert.Contains(integrationEvent.Items, item => item.Sku == "TEST-SKU-001" && item.Quantity == 5);
        Assert.Contains(integrationEvent.Items, item => item.Sku == "TEST-SKU-002" && item.Quantity == 3);
    }
}
