using RadiatorStockAPI.DTOs;

namespace RadiatorStockAPI.Services
{
    public interface IRadiatorService
    {
        Task<RadiatorResponseDto?> CreateRadiatorAsync(CreateRadiatorDto dto);
        Task<RadiatorResponseDto?> CreateRadiatorWithImageAsync(CreateRadiatorWithImageDto dto);

        Task<List<RadiatorListDto>> GetAllRadiatorsAsync();
        Task<RadiatorResponseDto?> GetRadiatorByIdAsync(Guid id);
        Task<RadiatorResponseDto?> UpdateRadiatorAsync(Guid id, UpdateRadiatorDto dto);
        Task<bool> DeleteRadiatorAsync(Guid id);
        Task<bool> RadiatorExistsAsync(Guid id);
        Task<bool> CodeExistsAsync(string code, Guid? excludeId = null);
        Task<List<RadiatorListDto>> UpdateMultiplePricesAsync(List<UpdateRadiatorPriceDto> updates);

        Task<RadiatorImageDto?> AddImageToRadiatorAsync(Guid radiatorId, UploadRadiatorImageDto dto);
        Task<List<RadiatorImageDto>> GetRadiatorImagesAsync(Guid radiatorId);
        Task<bool> DeleteRadiatorImageAsync(Guid radiatorId, Guid imageId);
        Task<bool> SetPrimaryImageAsync(Guid radiatorId, Guid imageId);

        Task<string> TestS3Async(IFormFile file);
    }
}
