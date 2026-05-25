using System.Text.Json;
using LogisticsHub.IntegrationEvents.StockReservations;
using LogisticsHub.InventoryService.Application.Persistence;
using LogisticsHub.InventoryService.Domain.Entities;
using LogisticsHub.InventoryService.Domain.Enums;
using LogisticsHub.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LogisticsHub.InventoryService.Application.StockReservations;

public sealed class CreateStockReservation : IRequestHandler<CreateStockReservationCommand, CreateStockReservationResult>
{
    private const int MaxConcurrencyAttempts = 3;
    private const string StockReservationRequestedEventType = "StockReservationRequestedIntegrationEvent";

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
            StockReservationErrors.ConcurrencyFailure,
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
                new CreateStockReservationResult(null, Error.None, AlreadyProcessed: true));
        }

        var now = DateTime.UtcNow;

        var validationError = ValidateCommand(command);
        if (validationError is not null)
        {
            return StockReservationAttemptResult.Completed(
                await SaveFailureResultAsync(command, validationError, now, cancellationToken));
        }

        var inventoryItemsBySku = await GetInventoryItemsBySkuAsync(command, cancellationToken);

        var stockValidationError = ValidateStockAvailability(command, inventoryItemsBySku);
        if (stockValidationError is not null)
        {
            return StockReservationAttemptResult.Completed(
                await SaveFailureResultAsync(command, stockValidationError, now, cancellationToken));
        }

        var stockReservation = CreateReservation(command, inventoryItemsBySku, now);
        await _dbContext.AddStockReservationAsync(stockReservation, cancellationToken);

        var saveResult = await SaveSuccessfulResultAsync(command, stockReservation.Id, now, cancellationToken);

        if (saveResult == InventorySaveChangesResult.DuplicateInboxEvent)
        {
            return StockReservationAttemptResult.Completed(
                new CreateStockReservationResult(null, Error.None, AlreadyProcessed: true));
        }

        if (saveResult == InventorySaveChangesResult.ConcurrencyConflict)
        {
            return StockReservationAttemptResult.Retry();
        }

        return StockReservationAttemptResult.Completed(
            new CreateStockReservationResult(ToResult(stockReservation), Error.None));
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

    private static Error? ValidateCommand(CreateStockReservationCommand command)
    {
        if (command.EventId == Guid.Empty)
        {
            return StockReservationErrors.EventIdRequired;
        }

        if (command.ShipmentId == Guid.Empty)
        {
            return StockReservationErrors.ShipmentIdRequired;
        }

        if (command.Items.Count == 0)
        {
            return StockReservationErrors.ItemRequired;
        }

        var skus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in command.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Sku))
            {
                return StockReservationErrors.SkuRequired;
            }

            if (!skus.Add(item.Sku.Trim()))
            {
                return StockReservationErrors.DuplicateSku(item.Sku);
            }

            if (item.Quantity <= 0)
            {
                return StockReservationErrors.QuantityMustBeGreaterThanZero;
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

    private static Error? ValidateStockAvailability(
        CreateStockReservationCommand command,
        IReadOnlyDictionary<string, Item> inventoryItemsBySku)
    {
        foreach (var requestedItem in command.Items)
        {
            var requestedSku = requestedItem.Sku.Trim();

            if (!inventoryItemsBySku.TryGetValue(requestedSku, out var inventoryItem))
            {
                return StockReservationErrors.SkuDoesNotExist(requestedItem.Sku);
            }

            if (!inventoryItem.IsActive)
            {
                return StockReservationErrors.SkuInactive(requestedItem.Sku);
            }

            if (inventoryItem.StockBalance is null)
            {
                return StockReservationErrors.StockBalanceMissing(requestedItem.Sku);
            }

            var available = inventoryItem.StockBalance.OnHand - inventoryItem.StockBalance.Reserved;
            if (available < requestedItem.Quantity)
            {
                return StockReservationErrors.InsufficientStock(requestedItem.Sku);
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
        Error error,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (command.EventId == Guid.Empty)
        {
            await AddFailedOutboxMessageAsync(command, error.Description, now, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreateStockReservationResult(null, error);
        }

        if (!TryGetPersistableEventId(command.EventId, out var eventId))
        {
            return new CreateStockReservationResult(null, error);
        }

        await AddInboxMessageAsync(eventId, now, cancellationToken);
        await AddFailedOutboxMessageAsync(command, error.Description, now, cancellationToken);

        var saved = await _dbContext.SaveChangesAsyncHandlingDuplicateInboxEventAndConcurrencyAsync(cancellationToken);

        if (saved == InventorySaveChangesResult.DuplicateInboxEvent)
        {
            return new CreateStockReservationResult(null, Error.None, AlreadyProcessed: true);
        }

        return new CreateStockReservationResult(null, error);
    }

    private async Task AddInboxMessageAsync(
        Guid eventId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await _dbContext.AddInventoryInboxMessageAsync(CreateInboxMessage(eventId, now), cancellationToken);
    }

    private async Task AddReservedOutboxMessageAsync(
        CreateStockReservationCommand command,
        Guid reservationId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await _dbContext.AddInventoryOutboxMessageAsync(
            CreateReservedOutboxMessage(command, reservationId, now),
            cancellationToken);
    }

    private async Task AddFailedOutboxMessageAsync(
        CreateStockReservationCommand command,
        string reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await _dbContext.AddInventoryOutboxMessageAsync(
            CreateFailedOutboxMessage(command, reason, now),
            cancellationToken);
    }

    private static InventoryInboxMessage CreateInboxMessage(Guid eventId, DateTime now)
    {
        return new InventoryInboxMessage
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Type = StockReservationRequestedEventType,
            ProcessedAtUtc = now,
            CreatedAtUtc = now
        };
    }

    private static InventoryOutboxMessage CreateReservedOutboxMessage(
        CreateStockReservationCommand command,
        Guid reservationId,
        DateTime now)
    {
        var integrationEvent = new StockReservedIntegrationEvent(
            Guid.NewGuid(),
            now,
            command.ShipmentId,
            reservationId);

        return CreateOutboxMessage(
            integrationEvent.EventId,
            integrationEvent.OccurredAtUtc,
            typeof(StockReservedIntegrationEvent).FullName!,
            StockReservationRoutingKeys.Reserved,
            integrationEvent,
            now);
    }

    private static InventoryOutboxMessage CreateFailedOutboxMessage(
        CreateStockReservationCommand command,
        string reason,
        DateTime now)
    {
        var integrationEvent = new StockReservationFailedIntegrationEvent(
            Guid.NewGuid(),
            now,
            command.ShipmentId,
            reason);

        return CreateOutboxMessage(
            integrationEvent.EventId,
            integrationEvent.OccurredAtUtc,
            typeof(StockReservationFailedIntegrationEvent).FullName!,
            StockReservationRoutingKeys.Failed,
            integrationEvent,
            now);
    }

    private static InventoryOutboxMessage CreateOutboxMessage<TMessage>(
        Guid id,
        DateTime occurredAtUtc,
        string type,
        string routingKey,
        TMessage message,
        DateTime now)
    {
        return new InventoryOutboxMessage
        {
            Id = id,
            OccurredAtUtc = occurredAtUtc,
            Type = type,
            RoutingKey = routingKey,
            Payload = JsonSerializer.Serialize(message, JsonSerializerOptions),
            CreatedAtUtc = now
        };
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
                new CreateStockReservationResult(null, Error.None),
                ConcurrencyConflict: true);
        }
    }
}
