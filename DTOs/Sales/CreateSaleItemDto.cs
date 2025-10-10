using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Sales;

public class CreateSaleItemDto
{
    [Required]
    public Guid RadiatorId { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}
