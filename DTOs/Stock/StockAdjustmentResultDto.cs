namespace RadiatorStockAPI.DTOs.Stock;

public class StockAdjustmentResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Guid RadiatorId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string? AdjustmentReason { get; set; }
}
