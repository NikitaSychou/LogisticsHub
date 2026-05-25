using LogisticsHub.Results;

namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed record CreateStockReservationResult(
    StockReservationResult? Reservation,
    Error Error,
    bool AlreadyProcessed = false);
