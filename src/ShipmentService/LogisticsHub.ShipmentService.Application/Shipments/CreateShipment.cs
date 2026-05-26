using System.Text.Json;
using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.Results;
using LogisticsHub.ShipmentService.Application.Companies;
using LogisticsHub.ShipmentService.Application.Persistence;
using LogisticsHub.ShipmentService.Domain.Entities;
using LogisticsHub.ShipmentService.Domain.Enums;
using MediatR;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed class CreateShipment : IRequestHandler<CreateShipmentCommand, Result<CreateShipmentResult>>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IShipmentDbContext _dbContext;
    private readonly ICompanyAddressReferenceClient _companyAddressReferenceClient;

    public CreateShipment(
        IShipmentDbContext dbContext,
        ICompanyAddressReferenceClient companyAddressReferenceClient)
    {
        _dbContext = dbContext;
        _companyAddressReferenceClient = companyAddressReferenceClient;
    }

    public async Task<Result<CreateShipmentResult>> Handle(
        CreateShipmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.HasCompanyAddressReferences)
        {
            var validationError = await ValidateCompanyAddressReferencesAsync(command, cancellationToken);
            if (validationError is not null)
            {
                return Result<CreateShipmentResult>.Failure(validationError);
            }
        }

        var now = DateTime.UtcNow;
        var shipmentId = Guid.NewGuid();
        var shipmentNumber = $"SHP-{now:yyyyMMddHHmmssfff}-{shipmentId.ToString("N")[..8].ToUpperInvariant()}";

        var shipment = new Shipment
        {
            Id = shipmentId,
            Status = ShipmentStatus.ReservationRequested,
            ShipmentNumber = shipmentNumber,
            SenderCompanyId = command.SenderCompanyId,
            SenderAddressId = command.SenderAddressId,
            ReceiverCompanyId = command.ReceiverCompanyId,
            ReceiverAddressId = command.ReceiverAddressId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _dbContext.AddShipmentAsync(shipment, cancellationToken);

        foreach (var item in command.Items)
        {
            var shipmentItem = new ShipmentItem
            {
                ShipmentId = shipment.Id,
                Sku = item.Sku,
                Quantity = item.Quantity
            };

            await _dbContext.AddShipmentItemAsync(shipmentItem, cancellationToken);
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

        await _dbContext.AddShipmentOutboxMessageAsync(outboxMessage, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateShipmentResult>.Success(
            new CreateShipmentResult(
                shipment.Id,
                shipment.Status,
                shipment.SenderCompanyId,
                shipment.SenderAddressId,
                shipment.ReceiverCompanyId,
                shipment.ReceiverAddressId));
    }

    private async Task<Error?> ValidateCompanyAddressReferencesAsync(
        CreateShipmentCommand command,
        CancellationToken cancellationToken)
    {
        var senderResult = await _companyAddressReferenceClient.ValidateAddressAsync(
            command.SenderCompanyId!.Value,
            command.SenderAddressId!.Value,
            cancellationToken);

        if (senderResult.Status == CompanyAddressReferenceValidationStatus.DependencyUnavailable)
        {
            return ShipmentErrors.CompanyServiceUnavailable();
        }

        if (senderResult.Status == CompanyAddressReferenceValidationStatus.NotFound)
        {
            return ShipmentErrors.SenderCompanyAddressNotFound(
                command.SenderCompanyId.Value,
                command.SenderAddressId.Value);
        }

        var receiverResult = await _companyAddressReferenceClient.ValidateAddressAsync(
            command.ReceiverCompanyId!.Value,
            command.ReceiverAddressId!.Value,
            cancellationToken);

        if (receiverResult.Status == CompanyAddressReferenceValidationStatus.DependencyUnavailable)
        {
            return ShipmentErrors.CompanyServiceUnavailable();
        }

        if (receiverResult.Status == CompanyAddressReferenceValidationStatus.NotFound)
        {
            return ShipmentErrors.ReceiverCompanyAddressNotFound(
                command.ReceiverCompanyId.Value,
                command.ReceiverAddressId.Value);
        }

        return null;
    }
}
