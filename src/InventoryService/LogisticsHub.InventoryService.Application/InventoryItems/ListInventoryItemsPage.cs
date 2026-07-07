using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;
using LogisticsHub.Results;
using MediatR;

namespace LogisticsHub.InventoryService.Application.InventoryItems;

public sealed class ListInventoryItemsPage : IRequestHandler<ListInventoryItemsPageQuery, PagedResponse<InventoryItemResult>>
{
    private readonly IInventoryDbContext _dbContext;

    public ListInventoryItemsPage(IInventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<InventoryItemResult>> Handle(
        ListInventoryItemsPageQuery query,
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.ListItemsPageAsync(
            query.PageNumber,
            query.PageSize,
            cancellationToken);
        var results = items
            .Take(query.PageSize)
            .Select(ToResult)
            .ToArray();

        return new PagedResponse<InventoryItemResult>(
            results,
            query.PageNumber,
            query.PageSize,
            items.Count > query.PageSize);
    }

    private static InventoryItemResult ToResult(Item item)
    {
        var quantityAvailable = item.StockBalance is null
            ? 0
            : item.StockBalance.OnHand - item.StockBalance.Reserved;

        return new InventoryItemResult(item.Sku, item.Name, quantityAvailable);
    }
}
