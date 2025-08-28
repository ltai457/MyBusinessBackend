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
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<StockLevel> StockLevels { get; set; } = new List<StockLevel>();
    }
}