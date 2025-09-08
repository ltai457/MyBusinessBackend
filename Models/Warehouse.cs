// Models/Warehouse.cs
using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.Models
{
    public class Warehouse
    {
        public Guid Id { get; set; }
    
        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;
    
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
    
        // Add these if they exist in your database:
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
    
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
        // Navigation properties
        public virtual ICollection<StockLevel> StockLevels { get; set; } = new List<StockLevel>();
    }
}