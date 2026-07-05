using LogisticsHub.ShipmentService.Application.Shipments;
using LogisticsHub.ShipmentService.Contracts;
using Riok.Mapperly.Abstractions;

namespace LogisticsHub.ShipmentService.Mapping;

[Mapper]
public static partial class ShipmentMapper
{
    public static CreateShipmentCommand ToCommand(CreateShipmentRequest request)
    {
        return new CreateShipmentCommand(
            request.Items!
                .Select(item => ToCommandItem(item!))
                .ToArray(),
            request.SenderCompanyId!.Value,
            request.SenderAddressId!.Value,
            request.ReceiverCompanyId!.Value,
            request.ReceiverAddressId!.Value);
    }

    public static partial CreateShipmentResponse ToResponse(CreateShipmentResult result);

    public static partial GetShipmentResponse ToResponse(GetShipmentResult result);

    private static CreateShipmentItemCommand ToCommandItem(CreateShipmentItemRequest item)
    {
        return new CreateShipmentItemCommand(item.Sku!, item.Quantity);
    }

    private static partial GetShipmentItemResponse ToResponseItem(GetShipmentItemResult item);
}
