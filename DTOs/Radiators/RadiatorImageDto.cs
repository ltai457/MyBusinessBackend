namespace RadiatorStockAPI.DTOs.Radiators;

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
