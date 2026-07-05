using LogisticsHub.InventoryService.Application.InventoryItems;
using LogisticsHub.InventoryService.Contracts;
using Riok.Mapperly.Abstractions;

namespace LogisticsHub.InventoryService.Mapping;

[Mapper]
public static partial class InventoryItemMapper
{
    public static CreateInventoryItemCommand ToCommand(CreateInventoryItemRequest request)
    {
        return new CreateInventoryItemCommand(
            request.Sku!.Trim(),
            request.Name!.Trim(),
            request.QuantityAvailable);
    }

    public static partial CreateInventoryItemResponse ToCreateResponse(InventoryItemResult result);

    public static partial GetInventoryItemResponse ToGetResponse(InventoryItemResult result);
}
