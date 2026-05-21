namespace LogisticsHub.ShipmentService.Domain.Entities;

public sealed class ShipmentInboxMessage
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string Type { get; set; } = string.Empty;

    public DateTime ProcessedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
