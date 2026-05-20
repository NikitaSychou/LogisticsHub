namespace LogisticsHub.InventoryService.Domain.Entities;

public sealed class StockBalance
{
    public Guid ItemId { get; set; }

    public int OnHand { get; set; }

    public int Reserved { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Item? Item { get; set; }
}
