namespace LogisticsHub.ShipmentService.Contracts;

public sealed record CreateShipmentRequest(
    IReadOnlyCollection<CreateShipmentItemRequest> Items);

public sealed record CreateShipmentItemRequest(
    string Sku,
    int Quantity);
