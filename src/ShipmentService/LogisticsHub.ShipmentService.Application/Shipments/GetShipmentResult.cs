using LogisticsHub.ShipmentService.Domain.Enums;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed record GetShipmentResult(
    Guid ShipmentId,
    string ShipmentNumber,
    ShipmentStatus Status,
    Guid? ReservationId,
    string? ReservationFailureReason,
    Guid SenderCompanyId,
    Guid SenderAddressId,
    Guid ReceiverCompanyId,
    Guid ReceiverAddressId,
    string? Comment,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DispatchedAt,
    DateTime? CancelledAt,
    IReadOnlyCollection<GetShipmentItemResult> Items);
