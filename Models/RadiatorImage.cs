using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.Models
{
    public class RadiatorImage
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid RadiatorId { get; set; }
        
        [Required]
        public string FileName { get; set; } = string.Empty;
        
        [Required]
        public string S3Key { get; set; } = string.Empty;
        
        [Required]
        public string Url { get; set; } = string.Empty;
        
        public bool IsPrimary { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual Radiator Radiator { get; set; } = null!;
    }
}