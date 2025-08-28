using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs
{
    public class CreateCustomerDto
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
    }
    
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
    
    public class CustomerResponseDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalPurchases { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
    }
    
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
}