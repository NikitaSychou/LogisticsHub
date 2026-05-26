using LogisticsHub.Results;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public static class ShipmentErrors
{
    public static Error SenderCompanyAddressNotFound(Guid companyId, Guid addressId)
    {
        return new Error(
            "shipment.sender_company_address_not_found",
            "Sender company/address reference was not found.",
            new Dictionary<string, object?>
            {
                ["companyId"] = companyId,
                ["addressId"] = addressId
            });
    }

    public static Error ReceiverCompanyAddressNotFound(Guid companyId, Guid addressId)
    {
        return new Error(
            "shipment.receiver_company_address_not_found",
            "Receiver company/address reference was not found.",
            new Dictionary<string, object?>
            {
                ["companyId"] = companyId,
                ["addressId"] = addressId
            });
    }

    public static Error CompanyServiceUnavailable()
    {
        return new Error(
            "shipment.company_service_unavailable",
            "CompanyService is unavailable while validating shipment company/address references.");
    }

    public static Error NotFound(Guid shipmentId)
    {
        return new Error(
            "shipment.not_found",
            "Shipment was not found.",
            new Dictionary<string, object?> { ["shipmentId"] = shipmentId });
    }
}
