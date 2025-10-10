using RadiatorStockAPI.DTOs.Customers;
using RadiatorStockAPI.DTOs.Users;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.DTOs.Sales;

public class SaleResponseDto
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public CustomerListDto Customer { get; set; } = null!;
    public UserDto ProcessedBy { get; set; } = null!;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public SaleStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime SaleDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SaleItemResponseDto> Items { get; set; } = new();
}
