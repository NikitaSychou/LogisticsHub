using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Application.Tests.Fakes;
using LogisticsHub.ShipmentService.Domain.Entities;
using LogisticsHub.ShipmentService.Domain.Enums;
using Xunit;

namespace LogisticsHub.ShipmentService.Application.Tests.Shipments;

public sealed class MarkShipmentReservationFailedTests
{
    [Fact]
    public async Task Handle_WhenShipmentIsReservationRequested_MarksShipmentReservationFailed()
    {
        // Arrange
        var dbContext = new FakeShipmentDbContext();
        var shipment = CreateShipment(ShipmentStatus.ReservationRequested);
        dbContext.Shipments.Add(shipment);

        var command = new MarkShipmentReservationFailedCommand(
            Guid.NewGuid(),
            shipment.Id,
            "Insufficient stock.");
        var handler = new MarkShipmentReservationFailed(dbContext);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ShipmentStatus.ReservationFailed, shipment.Status);
        Assert.Equal(command.Reason, shipment.ReservationFailureReason);
        Assert.Single(dbContext.InboxMessages);
        Assert.Equal(command.EventId, dbContext.InboxMessages[0].EventId);
    }

    [Fact]
    public async Task Handle_WhenShipmentIsNotReservationRequested_RecordsInboxWithoutChangingShipment()
    {
        // Arrange
        var dbContext = new FakeShipmentDbContext();
        var existingReservationId = Guid.NewGuid();
        var shipment = CreateShipment(ShipmentStatus.Reserved);
        shipment.ReservationId = existingReservationId;
        shipment.ReservationFailureReason = null;
        var originalUpdatedAt = shipment.UpdatedAt;
        dbContext.Shipments.Add(shipment);

        var command = new MarkShipmentReservationFailedCommand(
            Guid.NewGuid(),
            shipment.Id,
            "Late failure.");
        var handler = new MarkShipmentReservationFailed(dbContext);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ShipmentStatus.Reserved, shipment.Status);
        Assert.Equal(existingReservationId, shipment.ReservationId);
        Assert.Null(shipment.ReservationFailureReason);
        Assert.Equal(originalUpdatedAt, shipment.UpdatedAt);
        Assert.Single(dbContext.InboxMessages);
        Assert.Equal(command.EventId, dbContext.InboxMessages[0].EventId);
    }

    private static Shipment CreateShipment(ShipmentStatus status)
    {
        return new Shipment
        {
            Id = Guid.NewGuid(),
            ShipmentNumber = "SHP-TEST",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
