namespace RadiatorStockAPI.DTOs.Stock;

public class StockResponseDto
{
    public Dictionary<string, int> Stock { get; set; } = new();
}
