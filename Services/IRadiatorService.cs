// Services/IRadiatorService.cs
using RadiatorStockAPI.DTOs;

namespace RadiatorStockAPI.Services
{
    public interface IRadiatorService
    {
        Task<RadiatorResponseDto?> CreateRadiatorAsync(CreateRadiatorDto dto);
        Task<List<RadiatorListDto>> GetAllRadiatorsAsync();
        Task<RadiatorResponseDto?> GetRadiatorByIdAsync(Guid id);
        Task<RadiatorResponseDto?> UpdateRadiatorAsync(Guid id, UpdateRadiatorDto dto);
        Task<bool> DeleteRadiatorAsync(Guid id);

        Task<bool> RadiatorExistsAsync(Guid id);
        Task<bool> CodeExistsAsync(string code, Guid? excludeId = null);

        Task<List<RadiatorListDto>> UpdateMultiplePricesAsync(List<UpdateRadiatorPriceDto> updates);
    }
}