using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs
{
    public class CreateRadiatorDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Brand { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
        
        [Range(1900, 2030)]
        public int Year { get; set; }
    }
    
    public class UpdateRadiatorDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Brand { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
        
        [Range(1900, 2030)]
        public int Year { get; set; }
    }
    
    public class RadiatorResponseDto
    {
        public Guid Id { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Year { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Dictionary<string, int> Stock { get; set; } = new();
    }
    
    public class RadiatorListDto
    {
        public Guid Id { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Year { get; set; }
        public Dictionary<string, int> Stock { get; set; } = new();
    }
}