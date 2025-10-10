// Services/StockService.cs
// COMPLETE FILE - OPTIMIZED VERSION
using Microsoft.EntityFrameworkCore;
using RadiatorStockAPI.Data;
using RadiatorStockAPI.DTOs.Stock;
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

        // Get stock for a single radiator
        public async Task<StockResponseDto?> GetRadiatorStockAsync(Guid radiatorId)
        {
            var radiatorExists = await _context.Radiators.AnyAsync(r => r.Id == radiatorId);
            if (!radiatorExists)
                return null;

            var stock = await GetStockDictionaryAsync(radiatorId);
            return new StockResponseDto { Stock = stock };
        }

        // Update stock for a radiator in a specific warehouse
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

        // ✅✅✅ OPTIMIZED METHOD - THIS IS THE KEY FIX! ✅✅✅
        public async Task<IEnumerable<RadiatorWithStockDto>> GetAllRadiatorsWithStockAsync(
            string? search = null,
            bool lowStockOnly = false,
            string? warehouseCode = null)
        {
            _logger.LogInformation("📊 GetAllRadiatorsWithStockAsync - Optimized version starting...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // ✅ FIX 1: Use Include to load ALL related data in ONE query
            var query = _context.Radiators
                .Include(r => r.StockLevels)           // Load stock levels
                    .ThenInclude(sl => sl.Warehouse)   // Load warehouses
                .AsQueryable();

            // ✅ FIX 2: Apply search filter at DATABASE level (not in memory)
            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(r =>
                    r.Name.ToLower().Contains(searchLower) ||
                    r.Code.ToLower().Contains(searchLower) ||
                    r.Brand.ToLower().Contains(searchLower));
            }

            // ✅ FIX 3: Apply warehouse filter at DATABASE level
            if (!string.IsNullOrEmpty(warehouseCode))
            {
                var warehouseUpper = warehouseCode.ToUpper();
                query = query.Where(r =>
                    r.StockLevels.Any(sl => sl.Warehouse.Code == warehouseUpper));
            }

            // ✅ FIX 4: Apply low stock filter at DATABASE level
            if (lowStockOnly)
            {
                // Get radiators that have low stock (1-5) OR out of stock (0) in ANY warehouse
                query = query.Where(r =>
                    r.StockLevels.Any(sl => sl.Quantity >= 0 && sl.Quantity <= 5));
            }

            // ✅ FIX 5: Execute query ONCE - gets all data with relationships
            var radiators = await query.ToListAsync();

            stopwatch.Stop();
            _logger.LogInformation(
                "✅ Query executed in {ElapsedMs}ms, returned {Count} radiators",
                stopwatch.ElapsedMilliseconds,
                radiators.Count
            );

            // ✅ FIX 6: Map to DTO using already-loaded data (NO more database queries!)
            var result = radiators.Select(radiator =>
            {
                // Build stock dictionary from already-loaded StockLevels
                var stockDict = radiator.StockLevels.ToDictionary(
                    sl => sl.Warehouse.Code,
                    sl => sl.Quantity
                );

                // Calculate metrics from loaded data
                var totalStock = stockDict.Values.Sum();
                var hasLowStock = stockDict.Values.Any(q => q > 0 && q <= 5);
                var hasOutOfStock = stockDict.Values.Any(q => q == 0);

                return new RadiatorWithStockDto
                {
                    Id = radiator.Id,
                    Name = radiator.Name,
                    Code = radiator.Code,
                    Brand = radiator.Brand,
                    Year = radiator.Year,

                    // Pricing data
                    RetailPrice = radiator.RetailPrice,
                    TradePrice = radiator.TradePrice,
                    CostPrice = radiator.CostPrice,
                    IsPriceOverridable = radiator.IsPriceOverridable,
                    MaxDiscountPercent = radiator.MaxDiscountPercent,

                    // Stock data (already loaded, no DB query needed)
                    Stock = stockDict,
                    TotalStock = totalStock,
                    HasLowStock = hasLowStock,
                    HasOutOfStock = hasOutOfStock,
                    CreatedAt = radiator.CreatedAt,
                    UpdatedAt = radiator.UpdatedAt
                };
            }).ToList();

            _logger.LogInformation("✅ Mapping completed, returning {Count} DTOs", result.Count);
            return result;
        }

        // Get stock summary for dashboard
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

            var warehouseSummaries = warehouses.Select(w =>
            {
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

        // Get low stock items
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

        // Get out of stock items
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

        // Bulk update stock
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

        // Get warehouse stock
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

        // Get stock history
        public async Task<IEnumerable<StockHistoryDto>> GetStockHistoryAsync(
            Guid radiatorId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? warehouseCode = null)
        {
            // This would require a StockHistory table - placeholder implementation
            // You can implement this later if you add a StockHistory table to track changes
            _logger.LogInformation("Stock history requested but not implemented yet");
            return new List<StockHistoryDto>();
        }

        // Adjust stock (with reason tracking)
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

        // Helper: Get stock dictionary for a radiator
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

        // Helper: Log stock history
        private async Task LogStockHistoryAsync(
    Guid radiatorId,
    string warehouseCode,
    int oldQuantity,
    int newQuantity,
    string changeType,
    Guid? updatedBy,
    Guid? saleId = null,
    string? notes = null)
        {
            try
            {
                var warehouse = await _warehouseService.GetWarehouseByCodeAsync(warehouseCode);
                if (warehouse == null)
                {
                    _logger.LogWarning("Cannot log stock history: warehouse {WarehouseCode} not found", warehouseCode);
                    return;
                }

                var quantityChange = newQuantity - oldQuantity;
                var movementType = quantityChange >= 0 ? "INCOMING" : "OUTGOING";

                var stockHistory = new StockHistory
                {
                    Id = Guid.NewGuid(),
                    RadiatorId = radiatorId,
                    WarehouseId = warehouse.Id,
                    OldQuantity = oldQuantity,
                    NewQuantity = newQuantity,
                    QuantityChange = quantityChange,
                    MovementType = movementType,
                    ChangeType = changeType,
                    SaleId = saleId,
                    UpdatedBy = updatedBy,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.StockHistories.Add(stockHistory);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Stock history logged: {MovementType} for radiator {RadiatorId} in warehouse {WarehouseCode}: {OldQty} -> {NewQty}",
                    movementType, radiatorId, warehouseCode, oldQuantity, newQuantity
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging stock history for radiator {RadiatorId}", radiatorId);
            }
        }


        public async Task<IEnumerable<StockMovementDto>> GetStockMovementsAsync(
    Guid? radiatorId = null,
    string? warehouseCode = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    string? movementType = null,
    int? limit = null)
        {
            var query = _context.StockHistories
                .Include(sh => sh.Radiator)
                .Include(sh => sh.Warehouse)
                .Include(sh => sh.Sale)
                    .ThenInclude(s => s.Customer)
                .AsQueryable();

            if (radiatorId.HasValue)
                query = query.Where(sh => sh.RadiatorId == radiatorId.Value);

            if (!string.IsNullOrEmpty(warehouseCode))
            {
                var warehouse = await _warehouseService.GetWarehouseByCodeAsync(warehouseCode);
                if (warehouse != null)
                    query = query.Where(sh => sh.WarehouseId == warehouse.Id);
            }

            if (fromDate.HasValue)
                query = query.Where(sh => sh.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(sh => sh.CreatedAt <= toDate.Value);

            if (!string.IsNullOrEmpty(movementType))
                query = query.Where(sh => sh.MovementType == movementType.ToUpper());

            query = query.OrderByDescending(sh => sh.CreatedAt);

            if (limit.HasValue && limit.Value > 0)
                query = query.Take(limit.Value);

            var histories = await query.ToListAsync();

            return histories.Select(sh => new StockMovementDto
            {
                Id = sh.Id,
                Date = sh.CreatedAt,
                RadiatorId = sh.RadiatorId,
                ProductName = sh.Radiator.Name,
                ProductCode = sh.Radiator.Code,
                Brand = sh.Radiator.Brand,
                WarehouseId = sh.WarehouseId,
                WarehouseCode = sh.Warehouse.Code,
                WarehouseName = sh.Warehouse.Name,
                MovementType = sh.MovementType,
                Quantity = Math.Abs(sh.QuantityChange),
                OldQuantity = sh.OldQuantity,
                NewQuantity = sh.NewQuantity,
                ChangeType = sh.ChangeType,
                Notes = sh.Notes,
                SaleId = sh.SaleId,
                SaleNumber = sh.Sale?.SaleNumber,
                CustomerName = sh.Sale?.Customer != null
                    ? $"{sh.Sale.Customer.FirstName} {sh.Sale.Customer.LastName}".Trim()
                    : null
            }).ToList();
        }
    }
}
