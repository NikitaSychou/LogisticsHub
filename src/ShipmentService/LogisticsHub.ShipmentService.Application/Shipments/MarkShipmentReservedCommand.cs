using MediatR;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed record MarkShipmentReservedCommand(
    Guid EventId,
    Guid ShipmentId,
    Guid ReservationId) : IRequest;
