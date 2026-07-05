using LogisticsHub.ShipmentService.Domain.Enums;

namespace LogisticsHub.ShipmentService.Contracts;

public sealed record CreateShipmentResponse(
    Guid ShipmentId,
    ShipmentStatus Status,
    Guid SenderCompanyId,
    Guid SenderAddressId,
    Guid ReceiverCompanyId,
    Guid ReceiverAddressId);
