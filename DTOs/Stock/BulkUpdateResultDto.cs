namespace RadiatorStockAPI.DTOs.Stock;

public class BulkUpdateResultDto
{
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public bool HasErrors => ErrorCount > 0;
    public List<BulkUpdateErrorDto> Errors { get; set; } = new();
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
