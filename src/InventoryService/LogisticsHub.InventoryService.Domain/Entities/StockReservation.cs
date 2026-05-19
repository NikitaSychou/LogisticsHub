using LogisticsHub.InventoryService.Domain.Enums;

namespace LogisticsHub.InventoryService.Domain.Entities;

public class StockReservation
{
    public Guid Id { get; set; }
    public string? Sku { get; set; }
    public ReservationStatus Status { get; set; }
}
