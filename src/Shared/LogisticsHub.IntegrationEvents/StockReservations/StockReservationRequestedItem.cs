namespace LogisticsHub.IntegrationEvents.StockReservations;

public sealed record StockReservationRequestedItem(
    string Sku,
    int Quantity);
