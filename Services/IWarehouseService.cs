using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services
{
    public interface IWarehouseService
    {
        Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync();
        Task<Warehouse?> GetWarehouseByCodeAsync(string code);
        Task<bool> WarehouseExistsAsync(string code);
    }
}