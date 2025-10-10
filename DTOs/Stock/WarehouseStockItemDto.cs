namespace RadiatorStockAPI.DTOs.Stock;

public class WarehouseStockItemDto
{
    public Guid RadiatorId { get; set; }
    public string RadiatorName { get; set; } = string.Empty;
    public string RadiatorCode { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
