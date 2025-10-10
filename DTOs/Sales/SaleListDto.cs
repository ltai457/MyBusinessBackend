using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.DTOs.Sales;

public class SaleListDto
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProcessedByName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public SaleStatus Status { get; set; }
    public DateTime SaleDate { get; set; }
    public int ItemCount { get; set; }
}
