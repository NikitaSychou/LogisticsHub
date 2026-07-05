using LogisticsHub.ShipmentService.Domain.Enums;

namespace LogisticsHub.ShipmentService.Contracts;

public sealed record GetShipmentResponse(
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
    IReadOnlyCollection<GetShipmentItemResponse> Items);

public sealed record GetShipmentItemResponse(
    string Sku,
    int Quantity);
