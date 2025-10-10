namespace RadiatorStockAPI.DTOs.Customers;

public class CustomerListDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public bool IsActive { get; set; }
    public int TotalPurchases { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
}
