using RadiatorStockAPI.DTOs.Radiators;
using RadiatorStockAPI.DTOs.Warehouses;

namespace RadiatorStockAPI.DTOs.Sales;

public class SaleItemResponseDto
{
    public Guid Id { get; set; }
    public RadiatorListDto Radiator { get; set; } = null!;
    public WarehouseDto Warehouse { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
