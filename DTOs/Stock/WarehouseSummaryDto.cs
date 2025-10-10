namespace RadiatorStockAPI.DTOs.Stock;

public class WarehouseSummaryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalStock { get; set; }
    public int UniqueItems { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
}
