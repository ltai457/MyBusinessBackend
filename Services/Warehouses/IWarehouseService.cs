using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RadiatorStockAPI.DTOs.Warehouses;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services.Warehouses;

public interface IWarehouseService
{
    Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync();
    Task<Warehouse?> GetWarehouseByCodeAsync(string code);
    Task<bool> WarehouseExistsAsync(string code);
    Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id);
    Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto dto);
    Task<WarehouseDto> UpdateWarehouseAsync(Guid id, UpdateWarehouseDto dto);
    Task<bool> DeleteWarehouseAsync(Guid id);
    Task<bool> HasStockLevelsAsync(Guid warehouseId);
}
