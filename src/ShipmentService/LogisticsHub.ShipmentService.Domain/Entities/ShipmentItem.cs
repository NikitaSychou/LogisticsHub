namespace LogisticsHub.ShipmentService.Domain.Entities;

public class ShipmentItem
{
    public Guid ShipmentId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
