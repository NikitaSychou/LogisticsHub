using LogisticsHub.InventoryService.Domain.Enums;

namespace LogisticsHub.InventoryService.Domain.Entities;

public sealed class StockReservation
{
    public Guid Id { get; set; }

    public Guid ShipmentId { get; set; }

    public ReservationStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<StockReservationItem> Items { get; set; } = [];
}
