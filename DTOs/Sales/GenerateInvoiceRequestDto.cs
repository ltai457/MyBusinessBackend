using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Sales;

public class GenerateInvoiceRequestDto
{
    [Required]
    public InvoiceCustomerDto Customer { get; set; } = new();

    [Required]
    [MinLength(1)]
    public List<GenerateInvoiceItemDto> Items { get; set; } = new();

    [StringLength(20)]
    public string PaymentMethod { get; set; } = "Cash";

    [StringLength(500)]
    public string? Notes { get; set; }

    [Range(0, 1)]
    public decimal TaxRate { get; set; } = 0.15m;
}
