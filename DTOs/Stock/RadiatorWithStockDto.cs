namespace RadiatorStockAPI.DTOs.Stock;

public class RadiatorWithStockDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int Year { get; set; }

    public decimal RetailPrice { get; set; }
    public decimal? TradePrice { get; set; }
    public decimal? CostPrice { get; set; }
    public bool IsPriceOverridable { get; set; } = true;
    public decimal? MaxDiscountPercent { get; set; }

    public Dictionary<string, int> Stock { get; set; } = new();
    public int TotalStock { get; set; }
    public bool HasLowStock { get; set; }
    public bool HasOutOfStock { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
