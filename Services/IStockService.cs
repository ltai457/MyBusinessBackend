using RadiatorStockAPI.DTOs;

namespace RadiatorStockAPI.Services
{
    public interface IStockService
    {
        Task<StockResponseDto?> GetRadiatorStockAsync(Guid radiatorId);
        Task<bool> UpdateStockAsync(Guid radiatorId, UpdateStockDto dto);
        Task<Dictionary<string, int>> GetStockDictionaryAsync(Guid radiatorId);
    }
}