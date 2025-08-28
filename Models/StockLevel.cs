using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.Models
{
    public class StockLevel
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid RadiatorId { get; set; }
        
        [Required]
        public Guid WarehouseId { get; set; }
        
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Radiator Radiator { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
    }
}