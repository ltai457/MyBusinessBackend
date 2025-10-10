namespace RadiatorStockAPI.DTOs.Stock;

public class StockMovementDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public Guid RadiatorId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid? SaleId { get; set; }
    public string? SaleNumber { get; set; }
    public string? CustomerName { get; set; }
}
