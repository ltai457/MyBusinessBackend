using RadiatorStockAPI.DTOs;

namespace RadiatorStockAPI.Services
{
    public interface IStockService
    {
        Task<StockResponseDto?> GetRadiatorStockAsync(Guid radiatorId);
        Task<bool> UpdateStockAsync(Guid radiatorId, UpdateStockDto dto);
        Task<Dictionary<string, int>> GetStockDictionaryAsync(Guid radiatorId);
        
        Task<StockSummaryDto> GetStockSummaryAsync();
        Task<IEnumerable<RadiatorWithStockDto>> GetAllRadiatorsWithStockAsync(string? search = null, bool lowStockOnly = false, string? warehouseCode = null);
        Task<IEnumerable<LowStockItemDto>> GetLowStockItemsAsync(int threshold = 5);
        Task<IEnumerable<OutOfStockItemDto>> GetOutOfStockItemsAsync();
        Task<BulkUpdateResultDto> BulkUpdateStockAsync(BulkUpdateStockDto dto);
        Task<WarehouseStockDto?> GetWarehouseStockAsync(string warehouseCode);
        Task<IEnumerable<StockHistoryDto>> GetStockHistoryAsync(Guid radiatorId, DateTime? fromDate = null, DateTime? toDate = null, string? warehouseCode = null);
        Task<StockAdjustmentResultDto> AdjustStockAsync(StockAdjustmentDto dto);
        
    }
}