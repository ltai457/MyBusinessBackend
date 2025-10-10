namespace RadiatorStockAPI.DTOs.Stock;

public class BulkUpdateErrorDto
{
    public Guid RadiatorId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
