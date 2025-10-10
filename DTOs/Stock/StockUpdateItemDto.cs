using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Stock;

public class StockUpdateItemDto
{
    [Required]
    public Guid RadiatorId { get; set; }

    [Required]
    public string WarehouseCode { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }
}
