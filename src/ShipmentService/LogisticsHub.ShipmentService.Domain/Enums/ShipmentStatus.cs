namespace LogisticsHub.ShipmentService.Domain.Enums;

public enum ShipmentStatus
{
    Created = 0,
    ReservationRequested = 1,
    Reserved = 2,
    ReservationFailed = 3,
    Dispatched = 4,
    Cancelled = 5
}