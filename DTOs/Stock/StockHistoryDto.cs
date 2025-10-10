namespace RadiatorStockAPI.DTOs.Stock;

public class StockHistoryDto
{
    public Guid Id { get; set; }
    public Guid RadiatorId { get; set; }
    public string RadiatorName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public int ChangeAmount { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public Guid? UpdatedBy { get; set; }
    public string? UpdatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}
