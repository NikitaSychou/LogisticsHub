namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed record CreateShipmentItemCommand(
    string Sku,
    int Quantity);
