using LogisticsHub.InventoryService.Application.InventoryItems;
using LogisticsHub.InventoryService.Application.Tests.Fakes;
using LogisticsHub.InventoryService.Domain.Entities;
using Xunit;

namespace LogisticsHub.InventoryService.Application.Tests.InventoryItems;

public sealed class ListInventoryItemsPageTests
{
    [Fact]
    public async Task Handle_WhenMoreItemsExist_ReturnsFirstPageWithHasMore()
    {
        var dbContext = new FakeInventoryDbContext();
        dbContext.Items.Add(CreateItem("SKU-003", 10, 1));
        dbContext.Items.Add(CreateItem("SKU-001", 5, 0));
        dbContext.Items.Add(CreateItem("SKU-002", 7, 2));
        var handler = new ListInventoryItemsPage(dbContext);

        var result = await handler.Handle(new ListInventoryItemsPageQuery(1, 2), CancellationToken.None);

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.HasMore);
        Assert.Equal(["SKU-001", "SKU-002"], result.Items.Select(item => item.Sku));
        Assert.Equal([5, 5], result.Items.Select(item => item.QuantityAvailable));
    }

    [Fact]
    public async Task Handle_WhenFinalPageIsReturned_ReturnsHasMoreFalse()
    {
        var dbContext = new FakeInventoryDbContext();
        dbContext.Items.Add(CreateItem("SKU-001", 5, 0));
        dbContext.Items.Add(CreateItem("SKU-002", 7, 2));
        dbContext.Items.Add(CreateItem("SKU-003", 10, 1));
        var handler = new ListInventoryItemsPage(dbContext);

        var result = await handler.Handle(new ListInventoryItemsPageQuery(2, 2), CancellationToken.None);

        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.PageSize);
        Assert.False(result.HasMore);
        var item = Assert.Single(result.Items);
        Assert.Equal("SKU-003", item.Sku);
        Assert.Equal(9, item.QuantityAvailable);
    }

    private static Item CreateItem(string sku, int onHand, int reserved)
    {
        var itemId = Guid.NewGuid();

        return new Item
        {
            Id = itemId,
            Sku = sku,
            Name = $"Item {sku}",
            IsActive = true,
            StockBalance = new StockBalance
            {
                ItemId = itemId,
                OnHand = onHand,
                Reserved = reserved
            }
        };
    }
}
