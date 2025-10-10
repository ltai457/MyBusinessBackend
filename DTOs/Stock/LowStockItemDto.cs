namespace RadiatorStockAPI.DTOs.Stock;

public class LowStockItemDto
{
    public Guid RadiatorId { get; set; }
    public string RadiatorName { get; set; } = string.Empty;
    public string RadiatorCode { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int Threshold { get; set; }
    public DateTime LastUpdated { get; set; }
}
