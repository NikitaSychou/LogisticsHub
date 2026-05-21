namespace LogisticsHub.ShipmentService.Domain.Entities;

public sealed class ShipmentOutboxMessage
{
    public Guid Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string Type { get; set; } = string.Empty;

    public string RoutingKey { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTime? ProcessedAtUtc { get; set; }

    public string? Error { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
