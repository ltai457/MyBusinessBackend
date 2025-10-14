using RadiatorStockAPI.DTOs.Users;

namespace RadiatorStockAPI.DTOs.Sales;

public class InvoiceResponseDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public InvoiceCustomerDto Customer { get; set; } = new();
    public UserDto ProcessedBy { get; set; } = null!;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public decimal TaxRate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
}
