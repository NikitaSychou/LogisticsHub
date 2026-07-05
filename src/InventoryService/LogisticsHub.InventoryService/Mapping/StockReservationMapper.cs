using LogisticsHub.InventoryService.Application.StockReservations;
using LogisticsHub.InventoryService.Contracts;
using Riok.Mapperly.Abstractions;

namespace LogisticsHub.InventoryService.Mapping;

[Mapper]
public static partial class StockReservationMapper
{
    public static CreateStockReservationCommand ToCommand(CreateStockReservationRequest request)
    {
        return new CreateStockReservationCommand(
            request.ShipmentId,
            request.Items!
                .Select(ToCommandItem)
                .ToArray());
    }

    public static partial CreateStockReservationResponse ToCreateResponse(StockReservationResult result);

    public static partial GetStockReservationResponse ToGetResponse(StockReservationResult result);

    private static StockReservationItemCommand ToCommandItem(CreateStockReservationItemRequest item)
    {
        return new StockReservationItemCommand(item.Sku!.Trim(), item.Quantity);
    }

    private static partial StockReservationItemResponse ToResponseItem(StockReservationItemResult item);
}
