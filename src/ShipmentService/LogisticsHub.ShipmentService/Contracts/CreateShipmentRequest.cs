namespace LogisticsHub.ShipmentService.Contracts;

public sealed record CreateShipmentRequest(
    IReadOnlyCollection<CreateShipmentItemRequest> Items,
    Guid? SenderCompanyId,
    Guid? SenderAddressId,
    Guid? ReceiverCompanyId,
    Guid? ReceiverAddressId);

public sealed record CreateShipmentItemRequest(
    string Sku,
    int Quantity);
