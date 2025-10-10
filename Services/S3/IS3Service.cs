using Microsoft.AspNetCore.Http;

namespace RadiatorStockAPI.Services.S3;

public interface IS3Service
{
    Task<string> UploadImageAsync(IFormFile file);
    Task<bool> DeleteImageAsync(string key);
}
