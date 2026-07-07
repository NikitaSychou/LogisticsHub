using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed record IncreaseInventoryItemStockCommand(
    string Sku,
    int Quantity) : IRequest<Result<InventoryItemResult>>;
