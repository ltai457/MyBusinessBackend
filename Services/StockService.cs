using Microsoft.EntityFrameworkCore;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs;

namespace RadiatorStockAPI.Services
{
    public class StockService : IStockService
    {
        private readonly RadiatorDbContext _context;
        private readonly IWarehouseService _warehouseService;

        public StockService(RadiatorDbContext context, IWarehouseService warehouseService)
        {
            _context = context;
            _warehouseService = warehouseService;
        }

        public async Task<StockResponseDto?> GetRadiatorStockAsync(Guid radiatorId)
        {
            var radiatorExists = await _context.Radiators.AnyAsync(r => r.Id == radiatorId);
            if (!radiatorExists)
                return null;

            var stock = await GetStockDictionaryAsync(radiatorId);
            return new StockResponseDto { Stock = stock };
        }

        public async Task<bool> UpdateStockAsync(Guid radiatorId, UpdateStockDto dto)
        {
            // Verify radiator exists
            var radiatorExists = await _context.Radiators.AnyAsync(r => r.Id == radiatorId);
            if (!radiatorExists)
                return false;

            // Verify warehouse exists
            var warehouse = await _warehouseService.GetWarehouseByCodeAsync(dto.WarehouseCode);
            if (warehouse == null)
                return false;

            // Find or create stock level
            var stockLevel = await _context.StockLevels
                .FirstOrDefaultAsync(sl => sl.RadiatorId == radiatorId && sl.WarehouseId == warehouse.Id);

            if (stockLevel == null)
            {
                // Create new stock level if it doesn't exist
                stockLevel = new Models.StockLevel
                {
                    Id = Guid.NewGuid(),
                    RadiatorId = radiatorId,
                    WarehouseId = warehouse.Id,
                    Quantity = dto.Quantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.StockLevels.Add(stockLevel);
            }
            else
            {
                // Update existing stock level
                stockLevel.Quantity = dto.Quantity;
                stockLevel.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<string, int>> GetStockDictionaryAsync(Guid radiatorId)
        {
            var stockLevels = await _context.StockLevels
                .Include(sl => sl.Warehouse)
                .Where(sl => sl.RadiatorId == radiatorId)
                .ToListAsync();

            var stockDict = new Dictionary<string, int>();

            // Get all warehouses to ensure all are represented
            var allWarehouses = await _context.Warehouses.ToListAsync();
            
            foreach (var warehouse in allWarehouses)
            {
                var stockLevel = stockLevels.FirstOrDefault(sl => sl.WarehouseId == warehouse.Id);
                stockDict[warehouse.Code] = stockLevel?.Quantity ?? 0;
            }

            return stockDict;
        }
    }
}