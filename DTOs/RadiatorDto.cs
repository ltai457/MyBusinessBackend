using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs
{
    public class CreateRadiatorDto
    {
        [Required, StringLength(100)] public string Brand { get; set; } = string.Empty;
        [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
        [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
        [Range(1900, 2100)] public int Year { get; set; }
        [Range(0, double.MaxValue)] public decimal RetailPrice { get; set; }
        [Range(0, double.MaxValue)] public decimal? TradePrice { get; set; }
        [Range(0, double.MaxValue)] public decimal? CostPrice { get; set; }
        public bool IsPriceOverridable { get; set; } = true;
        [Range(0, 100)] public decimal? MaxDiscountPercent { get; set; } = 20;

        [StringLength(100)]
        public string? ProductType { get; set; }

        [StringLength(200)]
        public string? Dimensions { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateRadiatorDto
    {
        [StringLength(100)] public string? Brand { get; set; }
        [StringLength(50)] public string? Code { get; set; }
        [StringLength(200)] public string? Name { get; set; }
        [Range(1900, 2100)] public int? Year { get; set; }
        [Range(0, double.MaxValue)] public decimal? RetailPrice { get; set; }
        [Range(0, double.MaxValue)] public decimal? TradePrice { get; set; }
        [Range(0, double.MaxValue)] public decimal? CostPrice { get; set; }
        public bool? IsPriceOverridable { get; set; }
        [Range(0, 100)] public decimal? MaxDiscountPercent { get; set; }

        [StringLength(100)]
        public string? ProductType { get; set; }

        [StringLength(200)]
        public string? Dimensions { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateRadiatorPriceDto
    {
        [Required] public Guid Id { get; set; }
        [Range(0, double.MaxValue)] public decimal? RetailPrice { get; set; }
        [Range(0, double.MaxValue)] public decimal? TradePrice { get; set; }
        [Range(0, double.MaxValue)] public decimal? CostPrice { get; set; }
    }

    public class RadiatorListDto
    {
        public Guid Id { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal? TradePrice { get; set; }
        public bool IsPriceOverridable { get; set; }
        public decimal? MaxDiscountPercent { get; set; }
        public Dictionary<string, int> Stock { get; set; } = new();

        // New fields added
        public string? ProductType { get; set; }
        public string? Dimensions { get; set; }
        public string? Notes { get; set; }

        public string? PrimaryImageUrl { get; set; }
        public int ImageCount { get; set; }
    }

    public class RadiatorResponseDto
    {
        public Guid Id { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal? TradePrice { get; set; }
        public decimal? CostPrice { get; set; }
        public bool IsPriceOverridable { get; set; }
        public decimal? MaxDiscountPercent { get; set; }
        public Dictionary<string, int> Stock { get; set; } = new();

        // New fields added
        public string? ProductType { get; set; }
        public string? Dimensions { get; set; }
        public string? Notes { get; set; }

        public bool HasImage { get; set; }
        public string? ImageUrl { get; set; }
        public int ImageCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateRadiatorWithImageDto
    {
        [Required, StringLength(100)] public string Brand { get; set; } = string.Empty;
        [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
        [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
        [Range(1900, 2100)] public int Year { get; set; }
        [Range(0, double.MaxValue)] public decimal RetailPrice { get; set; }
        [Range(0, double.MaxValue)] public decimal? TradePrice { get; set; }
        [Range(0, double.MaxValue)] public decimal? CostPrice { get; set; }
        public bool IsPriceOverridable { get; set; } = true;
        [Range(0, 100)] public decimal? MaxDiscountPercent { get; set; } = 20;

        // New fields added
        [StringLength(100)]
        public string? ProductType { get; set; }

        [StringLength(200)]
        public string? Dimensions { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public Dictionary<string, int>? InitialStock { get; set; }
        public IFormFile? Image { get; set; }
    }

    public class RadiatorImageDto
    {
        public Guid Id { get; set; }
        public Guid RadiatorId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string S3Key { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UploadRadiatorImageDto
    {
        [Required] public IFormFile Image { get; set; } = null!;
        public bool IsPrimary { get; set; } = false;
        [StringLength(500)] public string? Description { get; set; }
    }
}