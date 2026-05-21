namespace LogisticsHub.IntegrationEvents.StockReservations;

public static class StockReservationRoutingKeys
{
    public const string Requested = "stock-reservation.requested";

    public const string Reserved = "stock-reservation.reserved";

    public const string Failed = "stock-reservation.failed";
}
