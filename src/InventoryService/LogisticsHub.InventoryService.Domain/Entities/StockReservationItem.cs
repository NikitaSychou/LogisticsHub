namespace LogisticsHub.InventoryService.Domain.Entities;

public sealed class StockReservationItem
{
    public Guid ReservationId { get; set; }

    public Guid ItemId { get; set; }

    public int Quantity { get; set; }

    public StockReservation? Reservation { get; set; }

    public Item? Item { get; set; }
}
