namespace RadiatorStockAPI.DTOs.Stock;

public class WarehouseStockDto
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int TotalStock { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public List<WarehouseStockItemDto> Items { get; set; } = new();
}
