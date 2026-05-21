using MediatR;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed record MarkShipmentReservationFailedCommand(
    Guid EventId,
    Guid ShipmentId,
    string Reason) : IRequest;
