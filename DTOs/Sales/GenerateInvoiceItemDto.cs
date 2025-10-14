using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Sales;

public class GenerateInvoiceItemDto
{
    /// <summary>
    /// Optional radiator reference for catalog items.
    /// Leave null to create a custom/free-text line.
    /// </summary>
    public Guid? RadiatorId { get; set; }

    /// <summary>
    /// Required when RadiatorId is provided. Ignored for custom lines.
    /// </summary>
    public Guid? WarehouseId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Optional override when referencing a radiator. Required for custom lines.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }
}
