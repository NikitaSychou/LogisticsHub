namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed record CreateStockReservationResult(
    StockReservationResult? Reservation,
    string? FailureReason,
    bool AlreadyProcessed = false);
