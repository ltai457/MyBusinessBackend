using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Customers;

public class UpdateCustomerDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Company { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}
