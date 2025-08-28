using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.Models
{
    public class Sale
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid CustomerId { get; set; }
        
        [Required]
        public Guid UserId { get; set; } // Who processed the sale
        
        [StringLength(50)]
        public string SaleNumber { get; set; } = string.Empty; // Auto-generated
        
        [Range(0, double.MaxValue)]
        public decimal SubTotal { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal TaxAmount { get; set; }
        
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }
        
        [StringLength(20)]
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Bank Transfer, etc.
        
        public SaleStatus Status { get; set; } = SaleStatus.Completed;
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual User ProcessedBy { get; set; } = null!;
        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
    
    public enum SaleStatus
    {
        Pending = 1,
        Completed = 2,
        Cancelled = 3,
        Refunded = 4
    }
}