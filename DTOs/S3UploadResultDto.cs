namespace RadiatorStockAPI.DTOs
{
    public class S3UploadResultDto
    {
        public bool Success { get; set; }
        public string? Key { get; set; }
        public string? Url { get; set; }
        public string? Error { get; set; }
    }
}