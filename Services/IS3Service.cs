namespace RadiatorStockAPI.Services
{
    public interface IS3Service
    {
        Task<string> UploadImageAsync(IFormFile file);
        Task<bool> DeleteImageAsync(string key);

       
    }
}
