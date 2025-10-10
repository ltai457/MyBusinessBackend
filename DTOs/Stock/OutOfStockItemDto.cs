namespace RadiatorStockAPI.DTOs.Stock;

public class OutOfStockItemDto
{
    public Guid RadiatorId { get; set; }
    public string RadiatorName { get; set; } = string.Empty;
    public string RadiatorCode { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime LastStockDate { get; set; }
}
