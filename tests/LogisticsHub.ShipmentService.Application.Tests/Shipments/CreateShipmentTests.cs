using System.Text.Json;
using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.ShipmentService.Application.Companies;
using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Application.Tests.Fakes;
using LogisticsHub.ShipmentService.Domain.Entities;
using LogisticsHub.ShipmentService.Domain.Enums;
using Xunit;

namespace LogisticsHub.ShipmentService.Application.Tests.Shipments;

public sealed class CreateShipmentTests
{
    [Fact]
    public async Task Handle_WithCompanyAddressReferences_CreatesShipmentAndStockReservationRequestedOutboxMessage()
    {
        // Arrange
        var dbContext = new FakeShipmentDbContext();
        var senderCompanyId = Guid.NewGuid();
        var senderAddressId = Guid.NewGuid();
        var receiverCompanyId = Guid.NewGuid();
        var receiverAddressId = Guid.NewGuid();
        var command = new CreateShipmentCommand(
            [
                new CreateShipmentItemCommand("TEST-SKU-001", 5),
                new CreateShipmentItemCommand("TEST-SKU-002", 3)
            ],
            senderCompanyId,
            senderAddressId,
            receiverCompanyId,
            receiverAddressId);
        var companyAddressReferenceClient = new FakeCompanyAddressReferenceClient();
        var handler = new CreateShipment(dbContext, companyAddressReferenceClient);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var shipment = Assert.Single(dbContext.Shipments);
        Assert.Equal(result.Value.ShipmentId, shipment.Id);
        Assert.Equal(ShipmentStatus.ReservationRequested, shipment.Status);
        Assert.Equal(ShipmentStatus.ReservationRequested, result.Value.Status);
        Assert.Equal(senderCompanyId, shipment.SenderCompanyId);
        Assert.Equal(senderAddressId, shipment.SenderAddressId);
        Assert.Equal(receiverCompanyId, shipment.ReceiverCompanyId);
        Assert.Equal(receiverAddressId, shipment.ReceiverAddressId);
        Assert.DoesNotContain(
            typeof(Shipment).GetProperties(),
            property => property.Name is "DestinationName" or "DestinationAddress");
        Assert.Equal(
            [(senderCompanyId, senderAddressId), (receiverCompanyId, receiverAddressId)],
            companyAddressReferenceClient.Requests);

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

    [Fact]
    public async Task Handle_WithCompanyAddressReferences_ValidatesAndStoresReferences()
    {
        // Arrange
        var dbContext = new FakeShipmentDbContext();
        var senderCompanyId = Guid.NewGuid();
        var senderAddressId = Guid.NewGuid();
        var receiverCompanyId = Guid.NewGuid();
        var receiverAddressId = Guid.NewGuid();
        var command = new CreateShipmentCommand(
            [new CreateShipmentItemCommand("TEST-SKU-001", 5)],
            senderCompanyId,
            senderAddressId,
            receiverCompanyId,
            receiverAddressId);
        var companyAddressReferenceClient = new FakeCompanyAddressReferenceClient();
        var handler = new CreateShipment(dbContext, companyAddressReferenceClient);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var shipment = Assert.Single(dbContext.Shipments);
        Assert.Equal(senderCompanyId, shipment.SenderCompanyId);
        Assert.Equal(senderAddressId, shipment.SenderAddressId);
        Assert.Equal(receiverCompanyId, shipment.ReceiverCompanyId);
        Assert.Equal(receiverAddressId, shipment.ReceiverAddressId);
        Assert.Equal(senderCompanyId, result.Value.SenderCompanyId);
        Assert.Equal(senderAddressId, result.Value.SenderAddressId);
        Assert.Equal(receiverCompanyId, result.Value.ReceiverCompanyId);
        Assert.Equal(receiverAddressId, result.Value.ReceiverAddressId);
        Assert.Equal(
            [(senderCompanyId, senderAddressId), (receiverCompanyId, receiverAddressId)],
            companyAddressReferenceClient.Requests);
    }

    [Fact]
    public async Task Handle_WithInvalidSenderCompanyAddressReference_ReturnsError()
    {
        // Arrange
        var dbContext = new FakeShipmentDbContext();
        var senderCompanyId = Guid.NewGuid();
        var senderAddressId = Guid.NewGuid();
        var command = CreateCommandWithReferences(
            senderCompanyId,
            senderAddressId,
            Guid.NewGuid(),
            Guid.NewGuid());
        var companyAddressReferenceClient = new FakeCompanyAddressReferenceClient();
        companyAddressReferenceClient.SetResult(
            senderCompanyId,
            senderAddressId,
            CompanyAddressReferenceValidationResult.NotFound);
        var handler = new CreateShipment(dbContext, companyAddressReferenceClient);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("shipment.sender_company_address_not_found", result.Error.Code);
        Assert.Empty(dbContext.Shipments);
        Assert.Single(companyAddressReferenceClient.Requests);
    }

    [Fact]
    public async Task Handle_WithInvalidReceiverCompanyAddressReference_ReturnsError()
    {
        // Arrange
        var dbContext = new FakeShipmentDbContext();
        var receiverCompanyId = Guid.NewGuid();
        var receiverAddressId = Guid.NewGuid();
        var command = CreateCommandWithReferences(
            Guid.NewGuid(),
            Guid.NewGuid(),
            receiverCompanyId,
            receiverAddressId);
        var companyAddressReferenceClient = new FakeCompanyAddressReferenceClient();
        companyAddressReferenceClient.SetResult(
            receiverCompanyId,
            receiverAddressId,
            CompanyAddressReferenceValidationResult.NotFound);
        var handler = new CreateShipment(dbContext, companyAddressReferenceClient);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("shipment.receiver_company_address_not_found", result.Error.Code);
        Assert.Empty(dbContext.Shipments);
        Assert.Equal(2, companyAddressReferenceClient.Requests.Count);
    }

    [Fact]
    public async Task Handle_WhenCompanyServiceUnavailable_ReturnsDependencyError()
    {
        // Arrange
        var dbContext = new FakeShipmentDbContext();
        var command = CreateCommandWithReferences(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());
        var companyAddressReferenceClient = new FakeCompanyAddressReferenceClient
        {
            DefaultResult = CompanyAddressReferenceValidationResult.DependencyUnavailable
        };
        var handler = new CreateShipment(dbContext, companyAddressReferenceClient);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("shipment.company_service_unavailable", result.Error.Code);
        Assert.Empty(dbContext.Shipments);
        Assert.Single(companyAddressReferenceClient.Requests);
    }

    private static CreateShipmentCommand CreateCommandWithReferences(
        Guid senderCompanyId,
        Guid senderAddressId,
        Guid receiverCompanyId,
        Guid receiverAddressId)
    {
        return new CreateShipmentCommand(
            [new CreateShipmentItemCommand("TEST-SKU-001", 5)],
            senderCompanyId,
            senderAddressId,
            receiverCompanyId,
            receiverAddressId);
    }
}
