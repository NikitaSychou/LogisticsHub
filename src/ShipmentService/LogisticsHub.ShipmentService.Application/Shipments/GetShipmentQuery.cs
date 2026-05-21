using MediatR;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed record GetShipmentQuery(Guid Id) : IRequest<GetShipmentResult?>;
