using MediatR;

namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed record CreateStockReservationCommand(
    Guid ShipmentId,
    IReadOnlyCollection<StockReservationItemCommand> Items,
    Guid? EventId = null) : IRequest<CreateStockReservationResult>;
