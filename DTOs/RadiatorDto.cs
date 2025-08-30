// DTOs/RadiatorDto.cs
using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs
{
    public class CreateRadiatorDto
    {
        [Required, StringLength(100)] public string Brand { get; set; } = string.Empty;

        [Required, StringLength(50)] public string Code { get; set; } = string.Empty;

        [Required, StringLength(200)] public string Name { get; set; } = string.Empty;

        [Range(1900, 2100)] public int Year { get; set; }

        // Prices
        [Range(0, double.MaxValue)] public decimal RetailPrice { get; set; }

        [Range(0, double.MaxValue)] public decimal? TradePrice { get; set; }

        [Range(0, double.MaxValue)] public decimal? CostPrice { get; set; }

        public bool IsPriceOverridable { get; set; } = true;

        [Range(0, 100)] public decimal? MaxDiscountPercent { get; set; } = 20;
    }

    public class UpdateRadiatorDto
    {
        [StringLength(100)] public string? Brand { get; set; }

        [StringLength(50)] public string? Code { get; set; }

        [StringLength(200)] public string? Name { get; set; }

        [Range(1900, 2100)] public int? Year { get; set; }

        // Prices
        [Range(0, double.MaxValue)] public decimal? RetailPrice { get; set; }

        [Range(0, double.MaxValue)] public decimal? TradePrice { get; set; }

        [Range(0, double.MaxValue)] public decimal? CostPrice { get; set; }

        public bool? IsPriceOverridable { get; set; }

        [Range(0, 100)] public decimal? MaxDiscountPercent { get; set; }
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

        // Prices for list view
        public decimal RetailPrice { get; set; }
        public decimal? TradePrice { get; set; }

        // ✅ Add these two so your service mapping compiles
        public bool IsPriceOverridable { get; set; }
        public decimal? MaxDiscountPercent { get; set; }

        // Stock by warehouse code, e.g. { "WH1": 3, "WH2": 0, "WH3": 5 }
        public Dictionary<string, int> Stock { get; set; } = new();
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

        // ✅ Add these two so your service mapping compiles
        public bool IsPriceOverridable { get; set; }
        public decimal? MaxDiscountPercent { get; set; }

        // Full stock breakdown
        public Dictionary<string, int> Stock { get; set; } = new();
    }
}
  
