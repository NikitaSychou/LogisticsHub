using LogisticsHub.ShipmentService.Domain.Enums;

namespace LogisticsHub.ShipmentService.Domain.Entities;

public sealed class Shipment
{
    public Guid Id { get; set; }

    public string ShipmentNumber { get; set; } = string.Empty;

    public ShipmentStatus Status { get; set; }

    public Guid? ReservationId { get; set; }

    public string? ReservationFailureReason { get; set; }

    public Guid SenderCompanyId { get; set; }

    public Guid SenderAddressId { get; set; }

    public Guid ReceiverCompanyId { get; set; }

    public Guid ReceiverAddressId { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? DispatchedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public List<ShipmentItem> Items { get; set; } = [];
}
