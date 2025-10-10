using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RadiatorStockAPI.DTOs.Radiators;

public class CreateRadiatorWithImageDto
{
    [Required, StringLength(100)]
    public string Brand { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(1900, 2100)]
    public int Year { get; set; }

    [Range(0, double.MaxValue)]
    public decimal RetailPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? TradePrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CostPrice { get; set; }

    public bool IsPriceOverridable { get; set; } = true;

    [Range(0, 100)]
    public decimal? MaxDiscountPercent { get; set; } = 20;

    [StringLength(100)]
    public string? ProductType { get; set; }

    [StringLength(200)]
    public string? Dimensions { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public Dictionary<string, int>? InitialStock { get; set; }
    public IFormFile? Image { get; set; }
}
