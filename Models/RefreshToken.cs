using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        
        [Required]
        public string Token { get; set; } = string.Empty;
        
        [Required]
        public Guid UserId { get; set; }
        
        public DateTime ExpiryDate { get; set; }
        
        public bool IsRevoked { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        
        public bool IsExpired => DateTime.UtcNow >= ExpiryDate;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}