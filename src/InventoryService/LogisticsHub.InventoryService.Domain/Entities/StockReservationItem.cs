namespace LogisticsHub.InventoryService.Domain.Entities;

public class StockReservationItem
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public string? Sku { get; set; }
    public int Quantity { get; set; }
}
