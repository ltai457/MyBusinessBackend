using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.Models
{
    public class InvoiceItem
    {
        public Guid Id { get; set; }

        [Required]
        public Guid InvoiceId { get; set; }

        // Optional - only set if this is a radiator product
        public Guid? RadiatorId { get; set; }
        public Guid? WarehouseId { get; set; }

        // For custom items or radiator items
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        // Stored radiator info at time of invoice (for audit trail)
        [StringLength(100)]
        public string? RadiatorCode { get; set; }

        [StringLength(200)]
        public string? RadiatorName { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        // Warehouse info
        [StringLength(50)]
        public string? WarehouseCode { get; set; }

        [StringLength(200)]
        public string? WarehouseName { get; set; }

        // Whether this is a custom item (not from radiator inventory)
        public bool IsCustomItem { get; set; } = false;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Invoice Invoice { get; set; } = null!;
        public virtual Radiator? Radiator { get; set; }
        public virtual Warehouse? Warehouse { get; set; }
    }
}
