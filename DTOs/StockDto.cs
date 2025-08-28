using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs
{
    public class UpdateStockDto
    {
        [Required]
        [StringLength(10)]
        public string WarehouseCode { get; set; } = string.Empty;
        
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
    }
    
    public class StockResponseDto
    {
        public Dictionary<string, int> Stock { get; set; } = new();
    }
}