using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;
using LogisticsHub.InventoryService.Domain.Enums;

namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed class CreateStockReservation
{
    private readonly IInventoryDbContext dbContext;

    public CreateStockReservation(IInventoryDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<CreateStockReservationResult> ExecuteAsync(
        CreateStockReservationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.EventId.HasValue)
        {
            var alreadyProcessed = await dbContext.HasInventoryInboxMessageAsync(
                command.EventId.Value,
                cancellationToken);

            if (alreadyProcessed)
            {
                return new CreateStockReservationResult(null, null, AlreadyProcessed: true);
            }
        }

        var requestedSkus = command.Items
            .Select(item => item.Sku)
            .ToArray();

        var inventoryItems = await dbContext.GetItemsBySkusAsync(requestedSkus, cancellationToken);
        var inventoryItemsBySku = inventoryItems.ToDictionary(item => item.Sku, StringComparer.OrdinalIgnoreCase);

        foreach (var requestedItem in command.Items)
        {
            if (!inventoryItemsBySku.TryGetValue(requestedItem.Sku, out var inventoryItem))
            {
                return new CreateStockReservationResult(null, $"SKU '{requestedItem.Sku}' does not exist.");
            }

            if (!inventoryItem.IsActive)
            {
                return new CreateStockReservationResult(null, $"SKU '{requestedItem.Sku}' is inactive.");
            }

            if (inventoryItem.StockBalance is null)
            {
                return new CreateStockReservationResult(null, $"SKU '{requestedItem.Sku}' has no stock balance.");
            }

            var available = inventoryItem.StockBalance.OnHand - inventoryItem.StockBalance.Reserved;
            if (available < requestedItem.Quantity)
            {
                return new CreateStockReservationResult(null, $"Insufficient stock for SKU '{requestedItem.Sku}'.");
            }
        }

        var now = DateTime.UtcNow;
        var stockReservation = new StockReservation
        {
            Id = Guid.NewGuid(),
            ShipmentId = command.ShipmentId,
            Status = ReservationStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var requestedItem in command.Items)
        {
            var inventoryItem = inventoryItemsBySku[requestedItem.Sku];

            inventoryItem.StockBalance!.Reserved += requestedItem.Quantity;
            inventoryItem.StockBalance.UpdatedAt = now;

            stockReservation.Items.Add(new StockReservationItem
            {
                ReservationId = stockReservation.Id,
                ItemId = inventoryItem.Id,
                Quantity = requestedItem.Quantity,
                Item = inventoryItem
            });
        }

        await dbContext.AddStockReservationAsync(stockReservation, cancellationToken);

        if (command.EventId.HasValue)
        {
            var inboxMessage = new InventoryInboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = command.EventId.Value,
                Type = "StockReservationRequestedIntegrationEvent",
                ProcessedAtUtc = now,
                CreatedAtUtc = now
            };

            await dbContext.AddInventoryInboxMessageAsync(inboxMessage, cancellationToken);
        }

        if (command.EventId.HasValue)
        {
            var saved = await dbContext.SaveChangesAsyncHandlingDuplicateInboxEventAsync(cancellationToken);

            if (!saved)
            {
                return new CreateStockReservationResult(null, null, AlreadyProcessed: true);
            }
        }
        else
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var resultItems = stockReservation.Items
            .Select(item => new StockReservationItemResult(item.Item!.Sku, item.Quantity))
            .ToArray();

        return new CreateStockReservationResult(
            new StockReservationResult(
                stockReservation.Id,
                stockReservation.ShipmentId,
                stockReservation.Status,
                resultItems),
            null);
    }
}
