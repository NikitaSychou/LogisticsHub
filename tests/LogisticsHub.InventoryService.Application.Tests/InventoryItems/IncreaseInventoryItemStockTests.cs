using LogisticsHub.InventoryService.Application.InventoryItems;
using LogisticsHub.InventoryService.Application.Tests.Fakes;
using LogisticsHub.InventoryService.Domain.Entities;
using Xunit;

namespace LogisticsHub.InventoryService.Application.Tests.InventoryItems;

public sealed class IncreaseInventoryItemStockTests
{
    [Fact]
    public async Task Handle_WhenItemExists_IncreasesOnHandStockAndReturnsUpdatedAvailability()
    {
        var dbContext = new FakeInventoryDbContext();
        var item = CreateItem("TEST-SKU", onHand: 10, reserved: 3);
        dbContext.Items.Add(item);
        var handler = new IncreaseInventoryItemStock(dbContext);

        var result = await handler.Handle(new IncreaseInventoryItemStockCommand("TEST-SKU", 5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, item.StockBalance!.OnHand);
        Assert.Equal(12, result.Value.QuantityAvailable);
        Assert.Equal(1, dbContext.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotExist_ReturnsNotFound()
    {
        var dbContext = new FakeInventoryDbContext();
        var handler = new IncreaseInventoryItemStock(dbContext);

        var result = await handler.Handle(new IncreaseInventoryItemStockCommand("MISSING-SKU", 5), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("inventory.item.not_found", result.Error.Code);
        Assert.Equal(0, dbContext.SaveChangesCallCount);
    }

    private static Item CreateItem(string sku, int onHand, int reserved)
    {
        var itemId = Guid.NewGuid();

        return new Item
        {
            Id = itemId,
            Sku = sku,
            Name = "Test item",
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
