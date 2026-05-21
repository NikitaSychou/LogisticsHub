using MediatR;

namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed record GetStockReservationQuery(Guid ReservationId) : IRequest<StockReservationResult?>;
