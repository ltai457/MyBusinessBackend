using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Sales;

public class CreateSaleDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [StringLength(20)]
    public string PaymentMethod { get; set; } = "Cash";

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateSaleItemDto> Items { get; set; } = new();
}
