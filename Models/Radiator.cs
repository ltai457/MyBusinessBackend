using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.Models
{
    public class Radiator
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Brand { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(1900, 2030)]
        public int Year { get; set; }

        // ===== EXISTING PRICING FIELDS =====
        [Range(0, double.MaxValue)]
        public decimal RetailPrice { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal? TradePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CostPrice { get; set; }

        public bool IsPriceOverridable { get; set; } = true;

        [Range(0, 100)]
        public decimal? MaxDiscountPercent { get; set; } = 20;

        // ===== NEW FIELDS - ADD THESE =====
        [StringLength(100)]
        public string? ProductType { get; set; }  // "Vehicle", "Truck", "Machinery", etc.
        
        [StringLength(200)]
        public string? Dimensions { get; set; }   // "250x240x40mm" or "500x600"
        
        [StringLength(500)]
        public string? Notes { get; set; }        // Additional product notes
        // ==================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<StockLevel> StockLevels { get; set; } = new List<StockLevel>();
        public virtual ICollection<RadiatorImage> Images { get; set; } = new List<RadiatorImage>(); 
    }
}