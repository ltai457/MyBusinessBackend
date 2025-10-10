using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Stock;

public class StockAdjustmentDto
{
    [Required]
    public Guid RadiatorId { get; set; }

    [Required]
    [StringLength(10)]
    public string WarehouseCode { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int NewQuantity { get; set; }

    [StringLength(250)]
    public string? Reason { get; set; }

    public Guid? UpdatedBy { get; set; }
}
