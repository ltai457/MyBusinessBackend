namespace RadiatorStockAPI.DTOs.Sales;

public class ReceiptDto
{
    public SaleResponseDto Sale { get; set; } = null!;
    public string CompanyName { get; set; } = "RadiatorStock NZ";
    public string CompanyAddress { get; set; } = "123 Main Street, Auckland, New Zealand";
    public string CompanyPhone { get; set; } = "+64 9 123 4567";
    public string CompanyEmail { get; set; } = "sales@radiatorstock.co.nz";
}
