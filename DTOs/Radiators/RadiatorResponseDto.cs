namespace RadiatorStockAPI.DTOs.Radiators;

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
    public string? ProductType { get; set; }
    public string? Dimensions { get; set; }
    public string? Notes { get; set; }
    public bool HasImage { get; set; }
    public string? ImageUrl { get; set; }
    public int ImageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
