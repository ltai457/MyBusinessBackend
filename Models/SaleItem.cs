using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.Models
{
    public class SaleItem
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid SaleId { get; set; }
        
        [Required]
        public Guid RadiatorId { get; set; }
        
        [Required]
        public Guid WarehouseId { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal TotalPrice { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Sale Sale { get; set; } = null!;
        public virtual Radiator Radiator { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
    }
}