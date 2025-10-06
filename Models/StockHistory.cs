using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.Models
{
    public class StockHistory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RadiatorId { get; set; }
        public Radiator Radiator { get; set; } = null!;

        [Required]
        public Guid WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        [Required]
        public int OldQuantity { get; set; }

        [Required]
        public int NewQuantity { get; set; }

        [Required]
        public int QuantityChange { get; set; }

        [Required]
        [MaxLength(50)]
        public string MovementType { get; set; } = string.Empty; // "INCOMING" or "OUTGOING"

        [Required]
        [MaxLength(100)]
        public string ChangeType { get; set; } = string.Empty; // "Manual Update", "Sale", etc.

        public Guid? SaleId { get; set; }
        public Sale? Sale { get; set; }

        public Guid? UpdatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}