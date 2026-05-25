using LogisticsHub.Results;

namespace LogisticsHub.InventoryService.Application.StockReservations;

public static class StockReservationErrors
{
    public static readonly Error EventIdRequired = new(
        "stock_reservation.event_id_required",
        "Event ID is required.");

    public static readonly Error ShipmentIdRequired = new(
        "stock_reservation.shipment_id_required",
        "Shipment ID is required.");

    public static readonly Error ItemRequired = new(
        "stock_reservation.item_required",
        "At least one item is required.");

    public static readonly Error SkuRequired = new(
        "stock_reservation.sku_required",
        "SKU is required.");

    public static readonly Error QuantityMustBeGreaterThanZero = new(
        "stock_reservation.quantity_must_be_greater_than_zero",
        "Quantity must be greater than zero.");

    public static readonly Error ConcurrencyFailure = new(
        "stock_reservation.concurrency_failure",
        "Stock reservation could not be completed due to concurrent inventory updates.");

    public static Error DuplicateSku(string sku)
    {
        return new Error(
            "stock_reservation.duplicate_sku",
            $"Duplicate SKU '{sku}' is not allowed.",
            new Dictionary<string, object?> { ["sku"] = sku });
    }

    public static Error SkuDoesNotExist(string sku)
    {
        return new Error(
            "stock_reservation.sku_not_found",
            $"SKU '{sku}' does not exist.",
            new Dictionary<string, object?> { ["sku"] = sku });
    }

    public static Error SkuInactive(string sku)
    {
        return new Error(
            "stock_reservation.sku_inactive",
            $"SKU '{sku}' is inactive.",
            new Dictionary<string, object?> { ["sku"] = sku });
    }

    public static Error StockBalanceMissing(string sku)
    {
        return new Error(
            "stock_reservation.stock_balance_missing",
            $"SKU '{sku}' has no stock balance.",
            new Dictionary<string, object?> { ["sku"] = sku });
    }

    public static Error InsufficientStock(string sku)
    {
        return new Error(
            "stock_reservation.insufficient_stock",
            $"Insufficient stock for SKU '{sku}'.",
            new Dictionary<string, object?> { ["sku"] = sku });
    }

    public static Error NotFound(Guid reservationId)
    {
        return new Error(
            "stock_reservation.not_found",
            "Stock reservation was not found.",
            new Dictionary<string, object?> { ["reservationId"] = reservationId });
    }
}
