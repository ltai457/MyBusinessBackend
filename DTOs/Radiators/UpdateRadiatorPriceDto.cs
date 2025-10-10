using System.ComponentModel.DataAnnotations;

namespace RadiatorStockAPI.DTOs.Radiators;

public class UpdateRadiatorPriceDto
{
    [Required]
    public Guid Id { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? RetailPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? TradePrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CostPrice { get; set; }
}
