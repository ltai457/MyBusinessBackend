using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.Models
{
    public class Invoice
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty; // Auto-generated INV{timestamp}{random}

        [Required]
        public Guid UserId { get; set; } // Who created the invoice

        // Customer information (stored directly, not linked to Customer table)
        [Required]
        [StringLength(150)]
        public string CustomerFullName { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(150)]
        public string? CustomerEmail { get; set; }

        [Phone]
        [StringLength(30)]
        public string? CustomerPhone { get; set; }

        [StringLength(200)]
        public string? CustomerCompany { get; set; }

        [StringLength(300)]
        public string? CustomerAddress { get; set; }

        // Financial details
        [Range(0, double.MaxValue)]
        public decimal SubTotal { get; set; }

        [Range(0, 1)]
        public decimal TaxRate { get; set; } = 0.15m;

        [Range(0, double.MaxValue)]
        public decimal TaxAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [StringLength(20)]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(500)]
        public string? Notes { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User CreatedBy { get; set; } = null!;
        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    }

    public enum InvoiceStatus
    {
        Draft = 1,
        Issued = 2,
        Paid = 3,
        Cancelled = 4
    }
}
