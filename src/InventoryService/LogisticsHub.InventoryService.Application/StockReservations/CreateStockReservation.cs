using System.Text.Json;
using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;
using LogisticsHub.InventoryService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed class CreateStockReservation : IRequestHandler<CreateStockReservationCommand, CreateStockReservationResult>
{
    private const int MaxConcurrencyAttempts = 3;
    private const string ConcurrencyFailureReason = "Stock reservation could not be completed due to concurrent inventory updates.";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IInventoryDbContext _dbContext;
    private readonly ILogger<CreateStockReservation> _logger;

    public CreateStockReservation(
        IInventoryDbContext dbContext,
        ILogger<CreateStockReservation> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CreateStockReservationResult> Handle(
        CreateStockReservationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        for (var attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
        {
            var attemptResult = await TryCreateStockReservationAsync(command, cancellationToken);

            if (!attemptResult.ConcurrencyConflict)
            {
                return attemptResult.Result;
            }

            _logger.LogWarning(
                "Stock reservation concurrency conflict for ShipmentId {ShipmentId} on attempt {Attempt} of {MaxAttempts}.",
                command.ShipmentId,
                attempt,
                MaxConcurrencyAttempts);
        }

        _logger.LogError(
            "Stock reservation failed for ShipmentId {ShipmentId} after {MaxAttempts} concurrency conflicts.",
            command.ShipmentId,
            MaxConcurrencyAttempts);

        return await SaveFailureResultAsync(
            command,
            ConcurrencyFailureReason,
            DateTime.UtcNow,
            cancellationToken);
    }

    private async Task<StockReservationAttemptResult> TryCreateStockReservationAsync(
        CreateStockReservationCommand command,
        CancellationToken cancellationToken)
    {
        if (await IsAlreadyProcessedAsync(command.EventId, cancellationToken))
        {
            return StockReservationAttemptResult.Completed(
                new CreateStockReservationResult(null, null, AlreadyProcessed: true));
        }

        var now = DateTime.UtcNow;

        var validationFailure = ValidateCommand(command);
        if (validationFailure is not null)
        {
            return StockReservationAttemptResult.Completed(
                await SaveFailureResultAsync(command, validationFailure, now, cancellationToken));
        }

        var inventoryItemsBySku = await GetInventoryItemsBySkuAsync(command, cancellationToken);

        var stockValidationFailure = ValidateStockAvailability(command, inventoryItemsBySku);
        if (stockValidationFailure is not null)
        {
            return StockReservationAttemptResult.Completed(
                await SaveFailureResultAsync(command, stockValidationFailure, now, cancellationToken));
        }

        var stockReservation = CreateReservation(command, inventoryItemsBySku, now);
        await _dbContext.AddStockReservationAsync(stockReservation, cancellationToken);

        var saveResult = await SaveSuccessfulResultAsync(command, stockReservation.Id, now, cancellationToken);

        if (saveResult == InventorySaveChangesResult.DuplicateInboxEvent)
        {
            return StockReservationAttemptResult.Completed(
                new CreateStockReservationResult(null, null, AlreadyProcessed: true));
        }

        if (saveResult == InventorySaveChangesResult.ConcurrencyConflict)
        {
            return StockReservationAttemptResult.Retry();
        }

        return StockReservationAttemptResult.Completed(
            new CreateStockReservationResult(ToResult(stockReservation), null));
    }

    private static bool TryGetPersistableEventId(Guid? eventId, out Guid value)
    {
        value = eventId.GetValueOrDefault();
        return eventId.HasValue && value != Guid.Empty;
    }

    private async Task<bool> IsAlreadyProcessedAsync(
        Guid? eventId,
        CancellationToken cancellationToken)
    {
        if (!TryGetPersistableEventId(eventId, out var value))
        {
            return false;
        }

        return await _dbContext.HasInventoryInboxMessageAsync(value, cancellationToken);
    }

    private static string? ValidateCommand(CreateStockReservationCommand command)
    {
        if (command.EventId == Guid.Empty)
        {
            return "Event ID is required.";
        }

        if (command.ShipmentId == Guid.Empty)
        {
            return "Shipment ID is required.";
        }

        if (command.Items.Count == 0)
        {
            return "At least one item is required.";
        }

        var skus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in command.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Sku))
            {
                return "SKU is required.";
            }

            if (!skus.Add(item.Sku.Trim()))
            {
                return $"Duplicate SKU '{item.Sku}' is not allowed.";
            }

            if (item.Quantity <= 0)
            {
                return "Quantity must be greater than zero.";
            }
        }

        return null;
    }

    private async Task<Dictionary<string, Item>> GetInventoryItemsBySkuAsync(
        CreateStockReservationCommand command,
        CancellationToken cancellationToken)
    {
        var requestedSkus = command.Items
            .Select(item => item.Sku.Trim())
            .ToArray();

        var inventoryItems = await _dbContext.GetItemsBySkusAsync(requestedSkus, cancellationToken);

        return inventoryItems.ToDictionary(item => item.Sku, StringComparer.OrdinalIgnoreCase);
    }

    private static string? ValidateStockAvailability(
        CreateStockReservationCommand command,
        IReadOnlyDictionary<string, Item> inventoryItemsBySku)
    {
        foreach (var requestedItem in command.Items)
        {
            var requestedSku = requestedItem.Sku.Trim();

            if (!inventoryItemsBySku.TryGetValue(requestedSku, out var inventoryItem))
            {
                return $"SKU '{requestedItem.Sku}' does not exist.";
            }

            if (!inventoryItem.IsActive)
            {
                return $"SKU '{requestedItem.Sku}' is inactive.";
            }

            if (inventoryItem.StockBalance is null)
            {
                return $"SKU '{requestedItem.Sku}' has no stock balance.";
            }

            var available = inventoryItem.StockBalance.OnHand - inventoryItem.StockBalance.Reserved;
            if (available < requestedItem.Quantity)
            {
                return $"Insufficient stock for SKU '{requestedItem.Sku}'.";
            }
        }

        return null;
    }

    private static StockReservation CreateReservation(
        CreateStockReservationCommand command,
        IReadOnlyDictionary<string, Item> inventoryItemsBySku,
        DateTime now)
    {
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
            var inventoryItem = inventoryItemsBySku[requestedItem.Sku.Trim()];

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

        return stockReservation;
    }

    private async Task<InventorySaveChangesResult> SaveSuccessfulResultAsync(
        CreateStockReservationCommand command,
        Guid reservationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!TryGetPersistableEventId(command.EventId, out var eventId))
        {
            return await _dbContext.SaveChangesAsyncHandlingDuplicateInboxEventAndConcurrencyAsync(cancellationToken);
        }

        await AddInboxMessageAsync(eventId, now, cancellationToken);
        await AddReservedOutboxMessageAsync(command, reservationId, now, cancellationToken);

        return await _dbContext.SaveChangesAsyncHandlingDuplicateInboxEventAndConcurrencyAsync(cancellationToken);
    }

    private static StockReservationResult ToResult(StockReservation stockReservation)
    {
        var resultItems = stockReservation.Items
            .Select(item => new StockReservationItemResult(item.Item!.Sku, item.Quantity))
            .ToArray();

        return new StockReservationResult(
            stockReservation.Id,
            stockReservation.ShipmentId,
            stockReservation.Status,
            resultItems);
    }

    private async Task<CreateStockReservationResult> SaveFailureResultAsync(
        CreateStockReservationCommand command,
        string reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (command.EventId == Guid.Empty)
        {
            await AddFailedOutboxMessageAsync(command, reason, now, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreateStockReservationResult(null, reason);
        }

        if (!TryGetPersistableEventId(command.EventId, out var eventId))
        {
            return new CreateStockReservationResult(null, reason);
        }

        await AddInboxMessageAsync(eventId, now, cancellationToken);
        await AddFailedOutboxMessageAsync(command, reason, now, cancellationToken);

        var saved = await _dbContext.SaveChangesAsyncHandlingDuplicateInboxEventAndConcurrencyAsync(cancellationToken);

        if (saved == InventorySaveChangesResult.DuplicateInboxEvent)
        {
            return new CreateStockReservationResult(null, null, AlreadyProcessed: true);
        }

        return new CreateStockReservationResult(null, reason);
    }

    private async Task AddInboxMessageAsync(
        Guid eventId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await _dbContext.AddInventoryInboxMessageAsync(
            new InventoryInboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Type = "StockReservationRequestedIntegrationEvent",
                ProcessedAtUtc = now,
                CreatedAtUtc = now
            },
            cancellationToken);
    }

    private async Task AddReservedOutboxMessageAsync(
        CreateStockReservationCommand command,
        Guid reservationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new StockReservedIntegrationEvent(
            Guid.NewGuid(),
            now,
            command.ShipmentId,
            reservationId);

        await AddOutboxMessageAsync(
            integrationEvent.EventId,
            integrationEvent.OccurredAtUtc,
            typeof(StockReservedIntegrationEvent).FullName!,
            StockReservationRoutingKeys.Reserved,
            integrationEvent,
            now,
            cancellationToken);
    }

    private async Task AddFailedOutboxMessageAsync(
        CreateStockReservationCommand command,
        string reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new StockReservationFailedIntegrationEvent(
            Guid.NewGuid(),
            now,
            command.ShipmentId,
            reason);

        await AddOutboxMessageAsync(
            integrationEvent.EventId,
            integrationEvent.OccurredAtUtc,
            typeof(StockReservationFailedIntegrationEvent).FullName!,
            StockReservationRoutingKeys.Failed,
            integrationEvent,
            now,
            cancellationToken);
    }

    private async Task AddOutboxMessageAsync<TMessage>(
        Guid id,
        DateTime occurredAtUtc,
        string type,
        string routingKey,
        TMessage message,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await _dbContext.AddInventoryOutboxMessageAsync(
            new InventoryOutboxMessage
            {
                Id = id,
                OccurredAtUtc = occurredAtUtc,
                Type = type,
                RoutingKey = routingKey,
                Payload = JsonSerializer.Serialize(message, JsonSerializerOptions),
                CreatedAtUtc = now
            },
            cancellationToken);
    }

    private sealed record StockReservationAttemptResult(
        CreateStockReservationResult Result,
        bool ConcurrencyConflict)
    {
        public static StockReservationAttemptResult Completed(CreateStockReservationResult result)
        {
            return new StockReservationAttemptResult(result, ConcurrencyConflict: false);
        }

        public static StockReservationAttemptResult Retry()
        {
            return new StockReservationAttemptResult(
                new CreateStockReservationResult(null, null),
                ConcurrencyConflict: true);
        }
    }
}
