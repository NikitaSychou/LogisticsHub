using MediatR;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed record GetInventoryItemQuery(string Sku) : IRequest<InventoryItemResult?>;
