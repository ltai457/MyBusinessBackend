// Services/IWarehouseService.cs - UPDATE YOUR EXISTING FILE
using RadiatorStockAPI.DTOs.Warehouses;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services
{
    public interface IWarehouseService
    {
        // Existing methods
        Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync();
        Task<Warehouse?> GetWarehouseByCodeAsync(string code);
        Task<bool> WarehouseExistsAsync(string code);

        // ADD THESE NEW METHODS:
        Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id);
        Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto);
        Task<WarehouseDto> UpdateWarehouseAsync(Guid id, UpdateWarehouseDto dto);
        Task<bool> DeleteWarehouseAsync(Guid id);
        Task<bool> HasStockLevelsAsync(Guid warehouseId);
    }
}
