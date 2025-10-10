namespace RadiatorStockAPI.DTOs.Stock;

public class StockSummaryDto
{
    public int TotalRadiators { get; set; }
    public int TotalStockItems { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public List<WarehouseSummaryDto> WarehouseSummaries { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
