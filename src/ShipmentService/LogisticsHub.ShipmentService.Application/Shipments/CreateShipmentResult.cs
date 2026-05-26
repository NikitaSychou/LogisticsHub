using LogisticsHub.ShipmentService.Domain.Enums;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed record CreateShipmentResult(
    Guid ShipmentId,
    ShipmentStatus Status,
    Guid? SenderCompanyId,
    Guid? SenderAddressId,
    Guid? ReceiverCompanyId,
    Guid? ReceiverAddressId);
