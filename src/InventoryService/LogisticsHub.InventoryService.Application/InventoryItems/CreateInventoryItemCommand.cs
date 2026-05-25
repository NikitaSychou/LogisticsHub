using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed record CreateInventoryItemCommand(
    string Sku,
    string Name,
    int QuantityAvailable) : IRequest<Result<InventoryItemResult>>;
