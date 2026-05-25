using LogisticsHub.Results;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public static class ShipmentErrors
{
    public static Error NotFound(Guid shipmentId)
    {
        return new Error(
            "shipment.not_found",
            "Shipment was not found.",
            new Dictionary<string, object?> { ["shipmentId"] = shipmentId });
    }
}
