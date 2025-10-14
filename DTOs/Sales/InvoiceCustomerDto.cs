using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Sales;

public class InvoiceCustomerDto
{
    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Company { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }
}
