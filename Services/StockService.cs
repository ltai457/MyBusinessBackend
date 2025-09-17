using Microsoft.EntityFrameworkCore;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs;
using RadiatorStockAPI.Models;

namespace RadiatorStockAPI.Services
{
    public class StockService : IStockService
    {
        private readonly RadiatorDbContext _context;
        private readonly IWarehouseService _warehouseService;
        private readonly ILogger<StockService> _logger;

        public StockService(RadiatorDbContext context, IWarehouseService warehouseService, ILogger<StockService> logger)
        {
            _context = context;
            _warehouseService = warehouseService;
            _logger = logger;
        }

        // Existing methods
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

            var oldQuantity = stockLevel?.Quantity ?? 0;

            if (stockLevel == null)
            {
                stockLevel = new StockLevel
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
                stockLevel.Quantity = dto.Quantity;
                stockLevel.UpdatedAt = DateTime.UtcNow;
            }

            // Log stock history
            await LogStockHistoryAsync(radiatorId, warehouse.Code, oldQuantity, dto.Quantity, "Manual Update", null);

            await _context.SaveChangesAsync();
            return true;
        }

        // New enhanced methods
        public async Task<StockSummaryDto> GetStockSummaryAsync()
        {
            var totalRadiators = await _context.Radiators.CountAsync();
            var warehouses = await _context.Warehouses.ToListAsync();
            
            var stockLevels = await _context.StockLevels
                .Include(sl => sl.Warehouse)
                .Include(sl => sl.Radiator)
                .ToListAsync();

            var totalStockItems = stockLevels.Sum(sl => sl.Quantity);
            var lowStockItems = stockLevels.Count(sl => sl.Quantity > 0 && sl.Quantity <= 5);
            var outOfStockItems = stockLevels.Count(sl => sl.Quantity == 0);

            var warehouseSummaries = warehouses.Select(w => {
                var warehouseStock = stockLevels.Where(sl => sl.WarehouseId == w.Id);
                return new WarehouseSummaryDto
                {
                    Code = w.Code,
                    Name = w.Name,
                    TotalStock = warehouseStock.Sum(sl => sl.Quantity),
                    UniqueItems = warehouseStock.Count(),
                    LowStockItems = warehouseStock.Count(sl => sl.Quantity > 0 && sl.Quantity <= 5),
                    OutOfStockItems = warehouseStock.Count(sl => sl.Quantity == 0)
                };
            }).ToList();

            return new StockSummaryDto
            {
                TotalRadiators = totalRadiators,
                TotalStockItems = totalStockItems,
                LowStockItems = lowStockItems,
                OutOfStockItems = outOfStockItems,
                WarehouseSummaries = warehouseSummaries
            };
        }

        public async Task<IEnumerable<RadiatorWithStockDto>> GetAllRadiatorsWithStockAsync(
    string? search = null, 
    bool lowStockOnly = false, 
    string? warehouseCode = null)
{
    // Start with radiators query that includes pricing
    var radiatorsQuery = _context.Radiators.AsQueryable();

    // Apply search filter
    if (!string.IsNullOrEmpty(search))
    {
        var searchLower = search.ToLower();
        radiatorsQuery = radiatorsQuery.Where(r => 
            r.Name.ToLower().Contains(searchLower) ||
            r.Code.ToLower().Contains(searchLower) ||
            r.Brand.ToLower().Contains(searchLower));
    }

    var radiators = await radiatorsQuery.ToListAsync();
    var result = new List<RadiatorWithStockDto>();

    foreach (var radiator in radiators)
    {
        // Get stock for this radiator
        var stockDict = await GetStockDictionaryAsync(radiator.Id);
        
        // Calculate stock metrics
        var totalStock = stockDict.Values.Sum();
        var hasLowStock = stockDict.Values.Any(q => q > 0 && q <= 5);
        var hasOutOfStock = stockDict.Values.Any(q => q == 0);

        // Apply filters
        if (lowStockOnly && !hasLowStock && !hasOutOfStock)
            continue;

        if (!string.IsNullOrEmpty(warehouseCode) && 
            !stockDict.ContainsKey(warehouseCode.ToUpper()))
            continue;

        // ✅ CREATE DTO WITH PRICING FIELDS
        var dto = new RadiatorWithStockDto
        {
            Id = radiator.Id,
            Name = radiator.Name,
            Code = radiator.Code,
            Brand = radiator.Brand,
            Year = radiator.Year,
            
            // ✅ INCLUDE PRICING DATA
            RetailPrice = radiator.RetailPrice,
            TradePrice = radiator.TradePrice,
            CostPrice = radiator.CostPrice,
            IsPriceOverridable = radiator.IsPriceOverridable,
            MaxDiscountPercent = radiator.MaxDiscountPercent,
            
            // Stock data
            Stock = stockDict,
            TotalStock = totalStock,
            HasLowStock = hasLowStock,
            HasOutOfStock = hasOutOfStock,
            CreatedAt = radiator.CreatedAt,
            UpdatedAt = radiator.UpdatedAt
        };

        result.Add(dto);
    }

    return result;
}

        public async Task<IEnumerable<LowStockItemDto>> GetLowStockItemsAsync(int threshold = 5)
        {
            return await _context.StockLevels
                .Where(sl => sl.Quantity > 0 && sl.Quantity <= threshold)
                .Include(sl => sl.Radiator)
                .Include(sl => sl.Warehouse)
                .Select(sl => new LowStockItemDto
                {
                    RadiatorId = sl.RadiatorId,
                    RadiatorName = sl.Radiator.Name,
                    RadiatorCode = sl.Radiator.Code,
                    Brand = sl.Radiator.Brand,
                    WarehouseCode = sl.Warehouse.Code,
                    WarehouseName = sl.Warehouse.Name,
                    CurrentStock = sl.Quantity,
                    Threshold = threshold,
                    LastUpdated = sl.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<OutOfStockItemDto>> GetOutOfStockItemsAsync()
        {
            return await _context.StockLevels
                .Where(sl => sl.Quantity == 0)
                .Include(sl => sl.Radiator)
                .Include(sl => sl.Warehouse)
                .Select(sl => new OutOfStockItemDto
                {
                    RadiatorId = sl.RadiatorId,
                    RadiatorName = sl.Radiator.Name,
                    RadiatorCode = sl.Radiator.Code,
                    Brand = sl.Radiator.Brand,
                    WarehouseCode = sl.Warehouse.Code,
                    WarehouseName = sl.Warehouse.Name,
                    LastStockDate = sl.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<BulkUpdateResultDto> BulkUpdateStockAsync(BulkUpdateStockDto dto)
        {
            var result = new BulkUpdateResultDto();

            foreach (var update in dto.Updates)
            {
                try
                {
                    var updateDto = new UpdateStockDto
                    {
                        WarehouseCode = update.WarehouseCode,
                        Quantity = update.Quantity
                    };

                    var success = await UpdateStockAsync(update.RadiatorId, updateDto);
                    
                    if (success)
                    {
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.ErrorCount++;
                        result.Errors.Add(new BulkUpdateErrorDto
                        {
                            RadiatorId = update.RadiatorId,
                            WarehouseCode = update.WarehouseCode,
                            Error = "Failed to update stock"
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add(new BulkUpdateErrorDto
                    {
                        RadiatorId = update.RadiatorId,
                        WarehouseCode = update.WarehouseCode,
                        Error = ex.Message
                    });
                }
            }

            return result;
        }

        public async Task<WarehouseStockDto?> GetWarehouseStockAsync(string warehouseCode)
        {
            var warehouse = await _warehouseService.GetWarehouseByCodeAsync(warehouseCode);
            if (warehouse == null)
                return null;

            var stockLevels = await _context.StockLevels
                .Where(sl => sl.WarehouseId == warehouse.Id)
                .Include(sl => sl.Radiator)
                .ToListAsync();

            var items = stockLevels.Select(sl => new WarehouseStockItemDto
            {
                RadiatorId = sl.RadiatorId,
                RadiatorName = sl.Radiator.Name,
                RadiatorCode = sl.Radiator.Code,
                Brand = sl.Radiator.Brand,
                Quantity = sl.Quantity,
                Status = sl.Quantity == 0 ? "Out" : sl.Quantity <= 5 ? "Low" : "Good",
                LastUpdated = sl.UpdatedAt
            }).ToList();

            return new WarehouseStockDto
            {
                WarehouseCode = warehouse.Code,
                WarehouseName = warehouse.Name,
                TotalItems = items.Count,
                TotalStock = items.Sum(i => i.Quantity),
                LowStockItems = items.Count(i => i.Status == "Low"),
                OutOfStockItems = items.Count(i => i.Status == "Out"),
                Items = items
            };
        }

        public async Task<IEnumerable<StockHistoryDto>> GetStockHistoryAsync(Guid radiatorId, DateTime? fromDate = null, DateTime? toDate = null, string? warehouseCode = null)
        {
            // This would require a StockHistory table - placeholder implementation
            return new List<StockHistoryDto>();
        }

        public async Task<StockAdjustmentResultDto> AdjustStockAsync(StockAdjustmentDto dto)
        {
            try
            {
                var updateDto = new UpdateStockDto
                {
                    WarehouseCode = dto.WarehouseCode,
                    Quantity = dto.NewQuantity
                };

                // Get old quantity for logging
                var currentStock = await GetRadiatorStockAsync(dto.RadiatorId);
                var oldQuantity = currentStock?.Stock.GetValueOrDefault(dto.WarehouseCode.ToUpper(), 0) ?? 0;

                var success = await UpdateStockAsync(dto.RadiatorId, updateDto);

                return new StockAdjustmentResultDto
                {
                    Success = success,
                    Error = success ? null : "Failed to adjust stock",
                    RadiatorId = dto.RadiatorId,
                    WarehouseCode = dto.WarehouseCode,
                    OldQuantity = oldQuantity,
                    NewQuantity = dto.NewQuantity,
                    AdjustmentReason = dto.Reason
                };
            }
            catch (Exception ex)
            {
                return new StockAdjustmentResultDto
                {
                    Success = false,
                    Error = ex.Message,
                    RadiatorId = dto.RadiatorId,
                    WarehouseCode = dto.WarehouseCode
                };
            }
        }

        // Helper methods
        public async Task<Dictionary<string, int>> GetStockDictionaryAsync(Guid radiatorId)
        {
            var stockLevels = await _context.StockLevels
                .Include(sl => sl.Warehouse)
                .Where(sl => sl.RadiatorId == radiatorId)
                .ToListAsync();

            return stockLevels.ToDictionary(
                sl => sl.Warehouse.Code,
                sl => sl.Quantity
            );
        }

        private async Task LogStockHistoryAsync(Guid radiatorId, string warehouseCode, int oldQuantity, int newQuantity, string changeType, Guid? updatedBy)
        {
            // This would require a StockHistory table - placeholder implementation
            _logger.LogInformation($"Stock changed for radiator {radiatorId} in warehouse {warehouseCode}: {oldQuantity} -> {newQuantity} ({changeType})");
        }
    }
}