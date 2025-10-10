using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Stock;

public class BulkUpdateStockDto
{
    [Required]
    public List<StockUpdateItemDto> Updates { get; set; } = new();
    public string? Reason { get; set; }
    public Guid? UpdatedBy { get; set; }
}
