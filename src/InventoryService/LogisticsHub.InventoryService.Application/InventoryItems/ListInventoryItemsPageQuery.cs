using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed record ListInventoryItemsPageQuery(
    int PageNumber,
    int PageSize) : IRequest<PagedResponse<InventoryItemResult>>;
