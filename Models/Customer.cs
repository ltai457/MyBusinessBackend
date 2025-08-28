using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.Models
{
    public class Customer
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
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
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    }
}