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
    public class StockSummaryDto
    {
        public int TotalRadiators { get; set; }
        public int TotalStockItems { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
        public List<WarehouseSummaryDto> WarehouseSummaries { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class WarehouseSummaryDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int TotalStock { get; set; }
        public int UniqueItems { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
    }

    public class RadiatorWithStockDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int Year { get; set; }

        // ✅ ADD THESE PRICING FIELDS
        public decimal RetailPrice { get; set; }
        public decimal? TradePrice { get; set; }
        public decimal? CostPrice { get; set; }
        public bool IsPriceOverridable { get; set; } = true;
        public decimal? MaxDiscountPercent { get; set; }

        // Existing stock fields
        public Dictionary<string, int> Stock { get; set; } = new();
        public int TotalStock { get; set; }
        public bool HasLowStock { get; set; }
        public bool HasOutOfStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class LowStockItemDto
    {
        public Guid RadiatorId { get; set; }
        public string RadiatorName { get; set; } = string.Empty;
        public string RadiatorCode { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int Threshold { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class OutOfStockItemDto
    {
        public Guid RadiatorId { get; set; }
        public string RadiatorName { get; set; } = string.Empty;
        public string RadiatorCode { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime LastStockDate { get; set; }
    }

    public class BulkUpdateStockDto
    {
        [Required]
        public List<StockUpdateItemDto> Updates { get; set; } = new();
        public string? Reason { get; set; }
        public Guid? UpdatedBy { get; set; }
    }

    public class StockUpdateItemDto
    {
        [Required]
        public Guid RadiatorId { get; set; }

        [Required]
        public string WarehouseCode { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class BulkUpdateResultDto
    {
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public bool HasErrors => ErrorCount > 0;
        public List<BulkUpdateErrorDto> Errors { get; set; } = new();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    public class BulkUpdateErrorDto
    {
        public Guid RadiatorId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    public class WarehouseStockDto
    {
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public int TotalStock { get; set; }
        public int LowStockItems { get; set; }
        public int OutOfStockItems { get; set; }
        public List<WarehouseStockItemDto> Items { get; set; } = new();
    }

    public class WarehouseStockItemDto
    {
        public Guid RadiatorId { get; set; }
        public string RadiatorName { get; set; } = string.Empty;
        public string RadiatorCode { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty; // "Good", "Low", "Out"
        public DateTime LastUpdated { get; set; }
    }

    public class StockHistoryDto
    {
        public Guid Id { get; set; }
        public Guid RadiatorId { get; set; }
        public string RadiatorName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public int OldQuantity { get; set; }
        public int NewQuantity { get; set; }
        public int ChangeAmount { get; set; }
        public string ChangeType { get; set; } = string.Empty; // "Increase", "Decrease", "Sale", "Adjustment"
        public string? Reason { get; set; }
        public Guid? UpdatedBy { get; set; }
        public string? UpdatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StockAdjustmentDto
    {
        [Required]
        public Guid RadiatorId { get; set; }

        [Required]
        public string WarehouseCode { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int NewQuantity { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public Guid? AdjustedBy { get; set; }
    }

    public class StockAdjustmentResultDto
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Guid RadiatorId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public int OldQuantity { get; set; }
        public int NewQuantity { get; set; }
        public string AdjustmentReason { get; set; } = string.Empty;
        public DateTime AdjustedAt { get; set; } = DateTime.UtcNow;
    }

    public class StockMovementDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }

        public Guid RadiatorId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;

        public Guid WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;

        public string MovementType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int OldQuantity { get; set; }
        public int NewQuantity { get; set; }

        public string ChangeType { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public Guid? SaleId { get; set; }
        public string? SaleNumber { get; set; }
        public string? CustomerName { get; set; }
    }
}
