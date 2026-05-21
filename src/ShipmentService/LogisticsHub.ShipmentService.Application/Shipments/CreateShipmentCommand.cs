using MediatR;

namespace LogisticsHub.ShipmentService.Application.Shipments;

public sealed record CreateShipmentCommand(
    IReadOnlyCollection<CreateShipmentItemCommand> Items) : IRequest<CreateShipmentResult>;
