namespace RadiatorStockAPI.DTOs.Sales;

public class InvoiceItemDto
{
    public Guid? RadiatorId { get; set; }
    public string RadiatorCode { get; set; } = string.Empty;
    public string RadiatorName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsCustomItem { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
