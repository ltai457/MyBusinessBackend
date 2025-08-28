using System.ComponentModel.DataAnnotations;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.DTOs
{
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
    
    public class CreateSaleItemDto
    {
        [Required]
        public Guid RadiatorId { get; set; }
        
        [Required]
        public Guid WarehouseId { get; set; }
        
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }
    
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
    
    public class SaleItemResponseDto
    {
        public Guid Id { get; set; }
        public RadiatorListDto Radiator { get; set; } = null!;
        public WarehouseDto Warehouse { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
    
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
    
    public class ReceiptDto
    {
        public SaleResponseDto Sale { get; set; } = null!;
        public string CompanyName { get; set; } = "RadiatorStock NZ";
        public string CompanyAddress { get; set; } = "123 Main Street, Auckland, New Zealand";
        public string CompanyPhone { get; set; } = "+64 9 123 4567";
        public string CompanyEmail { get; set; } = "sales@radiatorstock.co.nz";
    }
}