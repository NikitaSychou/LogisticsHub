namespace LogisticsHub.InventoryService.Domain.Entities;

public sealed class Item
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public StockBalance? StockBalance { get; set; }
}
