namespace LogisticsHub.InventoryService.Application.Persistence;

public enum InventorySaveChangesResult
{
    Saved,
    DuplicateInboxEvent,
    ConcurrencyConflict
}
