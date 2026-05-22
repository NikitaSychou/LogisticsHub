using System.Text.Json;
using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.InventoryService.Application.StockReservations;
using LogisticsHub.InventoryService.Application.Tests.Fakes;
using LogisticsHub.InventoryService.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LogisticsHub.InventoryService.Application.Tests.StockReservations;

public sealed class CreateStockReservationTests
{
    [Fact]
    public async Task Handle_WhenStockIsAvailable_CreatesReservationAndReservedOutboxMessage()
    {
        // Arrange
        var dbContext = new FakeInventoryDbContext();
        var item = CreateItem("TEST-SKU-001", onHand: 10, reserved: 2);
        dbContext.Items.Add(item);

        var eventId = Guid.NewGuid();
        var command = CreateCommand(eventId, quantity: 5);
        var handler = CreateHandler(dbContext);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.AlreadyProcessed);
        Assert.NotNull(result.Reservation);
        Assert.Null(result.FailureReason);
        Assert.Single(dbContext.StockReservations);
        Assert.Equal(7, item.StockBalance!.Reserved);
        Assert.Single(dbContext.InboxMessages);
        Assert.Equal(eventId, dbContext.InboxMessages[0].EventId);

        var outboxMessage = Assert.Single(dbContext.OutboxMessages);
        Assert.Equal(typeof(StockReservedIntegrationEvent).FullName, outboxMessage.Type);
        Assert.Equal(StockReservationRoutingKeys.Reserved, outboxMessage.RoutingKey);

        var integrationEvent = JsonSerializer.Deserialize<StockReservedIntegrationEvent>(
            outboxMessage.Payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(integrationEvent);
        Assert.Equal(command.ShipmentId, integrationEvent.ShipmentId);
        Assert.Equal(dbContext.StockReservations[0].Id, integrationEvent.ReservationId);
    }

    [Fact]
    public async Task Handle_WhenStockIsInsufficient_WritesFailureOutboxMessage()
    {
        // Arrange
        var dbContext = new FakeInventoryDbContext();
        var item = CreateItem("TEST-SKU-001", onHand: 10, reserved: 8);
        dbContext.Items.Add(item);

        var eventId = Guid.NewGuid();
        var command = CreateCommand(eventId, quantity: 5);
        var handler = CreateHandler(dbContext);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.AlreadyProcessed);
        Assert.Null(result.Reservation);
        Assert.Equal("Insufficient stock for SKU 'TEST-SKU-001'.", result.FailureReason);
        Assert.Empty(dbContext.StockReservations);
        Assert.Equal(8, item.StockBalance!.Reserved);
        Assert.Single(dbContext.InboxMessages);
        Assert.Equal(eventId, dbContext.InboxMessages[0].EventId);

        var outboxMessage = Assert.Single(dbContext.OutboxMessages);
        Assert.Equal(typeof(StockReservationFailedIntegrationEvent).FullName, outboxMessage.Type);
        Assert.Equal(StockReservationRoutingKeys.Failed, outboxMessage.RoutingKey);

        var integrationEvent = JsonSerializer.Deserialize<StockReservationFailedIntegrationEvent>(
            outboxMessage.Payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(integrationEvent);
        Assert.Equal(command.ShipmentId, integrationEvent.ShipmentId);
        Assert.Equal(result.FailureReason, integrationEvent.Reason);
    }

    [Fact]
    public async Task Handle_WhenEventIdWasAlreadyProcessed_DoesNotChangeState()
    {
        // Arrange
        var dbContext = new FakeInventoryDbContext();
        var item = CreateItem("TEST-SKU-001", onHand: 10, reserved: 2);
        dbContext.Items.Add(item);

        var eventId = Guid.NewGuid();
        dbContext.InboxMessages.Add(new InventoryInboxMessage
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Type = "StockReservationRequestedIntegrationEvent",
            ProcessedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        });

        var command = CreateCommand(eventId, quantity: 5);
        var handler = CreateHandler(dbContext);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.AlreadyProcessed);
        Assert.Null(result.Reservation);
        Assert.Null(result.FailureReason);
        Assert.Empty(dbContext.StockReservations);
        Assert.Equal(2, item.StockBalance!.Reserved);
        Assert.Single(dbContext.InboxMessages);
        Assert.Empty(dbContext.OutboxMessages);
    }

    [Fact]
    public async Task Handle_WhenEventIdIsEmpty_WritesFailureOutboxMessageWithoutInboxMessage()
    {
        // Arrange
        var dbContext = new FakeInventoryDbContext();
        var command = CreateCommand(Guid.Empty, quantity: 5);
        var handler = CreateHandler(dbContext);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.AlreadyProcessed);
        Assert.Null(result.Reservation);
        Assert.Equal("Event ID is required.", result.FailureReason);
        Assert.Empty(dbContext.InboxMessages);

        var outboxMessage = Assert.Single(dbContext.OutboxMessages);
        Assert.Equal(typeof(StockReservationFailedIntegrationEvent).FullName, outboxMessage.Type);
        Assert.Equal(StockReservationRoutingKeys.Failed, outboxMessage.RoutingKey);

        var integrationEvent = JsonSerializer.Deserialize<StockReservationFailedIntegrationEvent>(
            outboxMessage.Payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(integrationEvent);
        Assert.Equal(command.ShipmentId, integrationEvent.ShipmentId);
        Assert.Equal(result.FailureReason, integrationEvent.Reason);
    }

    private static CreateStockReservation CreateHandler(FakeInventoryDbContext dbContext)
    {
        return new CreateStockReservation(
            dbContext,
            NullLogger<CreateStockReservation>.Instance);
    }

    private static CreateStockReservationCommand CreateCommand(Guid eventId, int quantity)
    {
        return new CreateStockReservationCommand(
            Guid.NewGuid(),
            [new StockReservationItemCommand("TEST-SKU-001", quantity)],
            eventId);
    }

    private static Item CreateItem(string sku, int onHand, int reserved)
    {
        var itemId = Guid.NewGuid();
        return new Item
        {
            Id = itemId,
            Sku = sku,
            Name = "Test item",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StockBalance = new StockBalance
            {
                ItemId = itemId,
                OnHand = onHand,
                Reserved = reserved,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }
}
