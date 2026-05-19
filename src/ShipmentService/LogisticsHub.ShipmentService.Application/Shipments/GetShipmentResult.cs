using LogisticsHub.ShipmentService.Domain.Enums;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed record GetShipmentResult(
    Guid ShipmentId,
    string ShipmentNumber,
    ShipmentStatus Status,
    Guid? ReservationId,
    string? ReservationFailureReason,
    string DestinationName,
    string DestinationAddress,
    string? Comment,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DispatchedAt,
    DateTime? CancelledAt,
    IReadOnlyCollection<GetShipmentItemResult> Items);
