namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed record GetShipmentItemResult(
    string Sku,
    int Quantity);
